using System.Collections.Concurrent;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace SnippingTool.Services;

public sealed class ScreenRecordingService : IScreenRecordingService
{
    [DllImport("gdi32.dll")]
    private static extern bool BitBlt(
        IntPtr hdcDest, int xDest, int yDest, int width, int height,
        IntPtr hdcSrc, int xSrc, int ySrc, int rop);

    private const int SrcCopy = 0x00CC0020;

    private readonly ILogger<ScreenRecordingService> _logger;
    private readonly IMicrophoneDeviceService _microphoneDeviceService;
    private readonly IUserSettingsService _settings;
    private readonly IVideoWriterFactory _writerFactory;

    private IVideoWriter? _writer;
    private CancellationTokenSource? _cts;
    private Task? _captureLoop;
    private Task? _encodeLoop;
    private Channel<byte[]>? _encodeChannel;
    private readonly ConcurrentQueue<byte[]> _bufferPool = new();

    // Reused across every frame of a single recording session — allocated in Start, disposed in Stop.
    private Bitmap? _captureBitmap;
    private Graphics? _captureGraphics;

    private int _captureX;
    private int _captureY;
    private int _captureWidth;
    private int _captureHeight;
    private int _fps;
    private string? _activeMicrophoneDeviceName;
    private bool? _restoreMicrophoneMutedState;
    private readonly SemaphoreSlim _pauseGate = new SemaphoreSlim(1, 1);

    public bool IsRecording { get; private set; }
    public bool IsPaused { get; private set; }
    public bool IsRecordingMicrophoneEnabled { get; private set; }
    public bool CanToggleMicrophone { get; private set; }
    public bool IsMicrophoneMuted { get; private set; }

    public ScreenRecordingService(
        ILogger<ScreenRecordingService> logger,
        IMicrophoneDeviceService microphoneDeviceService,
        IUserSettingsService settings,
        IVideoWriterFactory writerFactory)
    {
        _logger = logger;
        _microphoneDeviceService = microphoneDeviceService;
        _settings = settings;
        _writerFactory = writerFactory;
    }

    public void Start(
        int x,
        int y,
        int width,
        int height,
        string outputPath)
    {
        var fps = _settings.Current.RecordingFps;
        _logger.LogInformation("Recording Start requested: region=({X},{Y},{W},{H}), fps={Fps}, path={Path}",
            x, y, width, height, fps, outputPath);

        if (IsRecording)
        {
            _logger.LogWarning("Start called while already recording — ignored");
            return;
        }

        // JPEG MCU requires even dimensions
        width = width % 2 == 0 ? width : width - 1;
        height = height % 2 == 0 ? height : height - 1;
        if (width <= 0 || height <= 0)
        {
            _logger.LogError("Region too small after even-dimension truncation: {W}x{H} — aborting", width, height);
            return;
        }

        _captureX = x;
        _captureY = y;
        _captureWidth = width;
        _captureHeight = height;
        _fps = fps;

        // Allocate the capture surface once per session — no per-frame allocation.
        _captureBitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        _captureGraphics = Graphics.FromImage(_captureBitmap);

        // Pre-allocate a small pool of raw frame buffers so neither the capture nor the
        // encode loop ever needs to allocate on the hot path.
        var bufferSize = width * height * 4;
        const int PoolSize = 4;
        for (var i = 0; i < PoolSize; i++)
        {
            _bufferPool.Enqueue(new byte[bufferSize]);
        }

        var microphoneDeviceName = ResolveMicrophoneDeviceName();
        _writer = _writerFactory.Create(width, height, fps, outputPath, microphoneDeviceName);
        IsRecordingMicrophoneEnabled = microphoneDeviceName is not null;
        _activeMicrophoneDeviceName = microphoneDeviceName;
        var initialMicrophoneMutedState = microphoneDeviceName is null
            ? null
            : _microphoneDeviceService.TryGetCaptureDeviceMuted(microphoneDeviceName);
        _restoreMicrophoneMutedState = initialMicrophoneMutedState;
        CanToggleMicrophone = initialMicrophoneMutedState.HasValue;
        IsMicrophoneMuted = initialMicrophoneMutedState ?? false;

        // Bounded channel: if the encode loop falls behind, CaptureFrameToChannel will
        // skip frames (TryWrite returns false) rather than stalling the capture thread.
        _encodeChannel = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(PoolSize)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true,
            SingleWriter = true,
        });

        _cts = new CancellationTokenSource();
        IsRecording = true;
        _captureLoop = Task.Run(() => CaptureLoop(_cts.Token));
        _encodeLoop = Task.Run(EncodeLoop);
        _logger.LogInformation("Recording started: {W}x{H} @ {Fps}fps (MP4) → {Path}", width, height, fps, outputPath);
    }

    public void Stop()
    {
        if (!IsRecording)
        {
            _logger.LogDebug("Stop called while not recording — ignored");
            return;
        }

        _logger.LogInformation("Stopping recording");
        IsRecording = false;
        if (IsPaused)
        {
            IsPaused = false;
            _pauseGate.Release();
        }

        _cts?.Cancel();

        // Complete the channel so the encode loop drains remaining buffered frames and exits.
        _encodeChannel?.Writer.TryComplete();

        try
        {
            _captureLoop?.Wait(TimeSpan.FromSeconds(3));
            _encodeLoop?.Wait(TimeSpan.FromSeconds(10));
        }
        catch (AggregateException ae) when (ae.InnerExceptions.All(ex => ex is OperationCanceledException))
        {
        }
        finally
        {
            _captureGraphics?.Dispose();
            _captureGraphics = null;
            _captureBitmap?.Dispose();
            _captureBitmap = null;
            IsRecordingMicrophoneEnabled = false;
            CanToggleMicrophone = false;
            IsMicrophoneMuted = false;
            while (_bufferPool.TryDequeue(out _))
            {
            }

            try
            {
                _writer?.Dispose();
                _logger.LogInformation("Writer closed — file finalised");
            }
            finally
            {
                RestoreMicrophoneMuteState();
                _writer = null;
            }
            _cts?.Dispose();
            _cts = null;
            _captureLoop = null;
            _encodeLoop = null;
            _encodeChannel = null;
        }
    }

    private async Task CaptureLoop(CancellationToken ct)
    {
        _logger.LogDebug("Capture loop started");
        var interval = TimeSpan.FromMilliseconds(1000.0 / _fps);
        using var timer = new PeriodicTimer(interval);
        var frameCount = 0;

        try
        {
            while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
            {
                await _pauseGate.WaitAsync(ct).ConfigureAwait(false);
                _pauseGate.Release();
                CaptureFrameToChannel();
                frameCount++;
                if (frameCount % 100 == 0)
                {
                    _logger.LogDebug("Captured {FrameCount} frames so far", frameCount);
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Capture loop failed after {FrameCount} frames", frameCount);
            throw;
        }
        finally
        {
            // Signal the encode loop that no more frames are coming.
            _encodeChannel?.Writer.TryComplete();
        }

        _logger.LogDebug("Capture loop ended after {FrameCount} frames", frameCount);
    }

    private void CaptureFrameToChannel()
    {
        if (_encodeChannel is null || _captureBitmap is null || _captureGraphics is null)
        {
            return;
        }

        // Rent a pre-allocated buffer; skip this frame if the pool is empty (encode is behind).
        if (!_bufferPool.TryDequeue(out var buffer))
        {
            _logger.LogDebug("Frame skipped — buffer pool exhausted");
            return;
        }

        // BitBlt is a direct GDI call, faster than the managed Graphics.CopyFromScreen wrapper.
        var bitmapDc = _captureGraphics.GetHdc();
        try
        {
            using var screenDc = new ScreenDc();
            BitBlt(bitmapDc, 0, 0, _captureWidth, _captureHeight,
                screenDc.Handle, _captureX, _captureY, SrcCopy);
        }
        finally
        {
            _captureGraphics.ReleaseHdc(bitmapDc);
        }

        var bits = _captureBitmap.LockBits(
            new Rectangle(0, 0, _captureWidth, _captureHeight),
            ImageLockMode.ReadOnly,
            PixelFormat.Format32bppArgb);
        try
        {
            Marshal.Copy(bits.Scan0, buffer, 0, buffer.Length);
        }
        finally
        {
            _captureBitmap.UnlockBits(bits);
        }

        // TryWrite never blocks; dropped frames are logged at debug level above.
        if (!_encodeChannel.Writer.TryWrite(buffer))
        {
            _bufferPool.Enqueue(buffer);
        }
    }

    // Runs on a dedicated background thread; WriteFrame may block on the pipe to ffmpeg
    // without ever affecting the capture timing or the UI thread.
    private async Task EncodeLoop()
    {
        _logger.LogDebug("Encode loop started");
        if (_encodeChannel is null)
        {
            return;
        }

        try
        {
            // CancellationToken.None: drain all buffered frames before exiting even after Stop().
            await foreach (var buffer in _encodeChannel.Reader.ReadAllAsync(CancellationToken.None).ConfigureAwait(false))
            {
                _writer?.WriteFrame(buffer);
                _bufferPool.Enqueue(buffer);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Encode loop failed");
        }

        _logger.LogDebug("Encode loop ended");
    }

    private string? ResolveMicrophoneDeviceName()
    {
        if (!_settings.Current.RecordMicrophone)
        {
            return null;
        }

        var microphoneDeviceName = _settings.Current.RecordingMicrophoneDeviceName;
        if (string.IsNullOrWhiteSpace(microphoneDeviceName))
        {
            microphoneDeviceName = _microphoneDeviceService.GetDefaultCaptureDeviceName();
        }

        var availableDevices = _microphoneDeviceService.GetAvailableCaptureDeviceNames();
        if (!string.IsNullOrWhiteSpace(microphoneDeviceName) &&
            availableDevices.Any(device => string.Equals(device, microphoneDeviceName, StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LogInformation("Microphone recording enabled using capture device '{DeviceName}'", microphoneDeviceName);
            return microphoneDeviceName;
        }

        microphoneDeviceName = _microphoneDeviceService.GetDefaultCaptureDeviceName();
        if (!string.IsNullOrWhiteSpace(microphoneDeviceName) &&
            availableDevices.Any(device => string.Equals(device, microphoneDeviceName, StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LogInformation("Configured microphone device was unavailable; falling back to default capture device '{DeviceName}'", microphoneDeviceName);
            return microphoneDeviceName;
        }

        if (string.IsNullOrWhiteSpace(microphoneDeviceName))
        {
            _logger.LogWarning("Microphone recording is enabled, but no default capture device is available. Continuing with video only.");
            return null;
        }

        _logger.LogWarning("Microphone recording is enabled, but no compatible capture device name could be resolved. Continuing with video only.");
        return null;
    }

    public void Pause()
    {
        if (!IsRecording || IsPaused)
        {
            return;
        }

        _pauseGate.Wait();
        IsPaused = true;
        _logger.LogInformation("Recording paused");
    }

    public void Resume()
    {
        if (!IsRecording || !IsPaused)
        {
            return;
        }

        IsPaused = false;
        _pauseGate.Release();
        _logger.LogInformation("Recording resumed");
    }

    public bool TrySetMicrophoneMuted(bool isMuted)
    {
        if (!CanToggleMicrophone || string.IsNullOrWhiteSpace(_activeMicrophoneDeviceName))
        {
            return false;
        }

        if (!_microphoneDeviceService.TrySetCaptureDeviceMuted(_activeMicrophoneDeviceName, isMuted))
        {
            _logger.LogWarning("Failed to set microphone mute state to {IsMuted} for active recording device '{DeviceName}'", isMuted, _activeMicrophoneDeviceName);
            return false;
        }

        IsMicrophoneMuted = isMuted;
        _logger.LogInformation("Recording microphone {State}", isMuted ? "muted" : "unmuted");
        return true;
    }

    public void Dispose() => Stop();

    private void RestoreMicrophoneMuteState()
    {
        if (string.IsNullOrWhiteSpace(_activeMicrophoneDeviceName) || !_restoreMicrophoneMutedState.HasValue)
        {
            _activeMicrophoneDeviceName = null;
            _restoreMicrophoneMutedState = null;
            return;
        }

        if (!_microphoneDeviceService.TrySetCaptureDeviceMuted(_activeMicrophoneDeviceName, _restoreMicrophoneMutedState.Value))
        {
            _logger.LogWarning("Failed to restore microphone mute state for recording device '{DeviceName}'", _activeMicrophoneDeviceName);
        }

        _activeMicrophoneDeviceName = null;
        _restoreMicrophoneMutedState = null;
    }

    // Lightweight RAII wrapper around the screen device context.
    private sealed class ScreenDc : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDc);

        public IntPtr Handle { get; }

        public ScreenDc() => Handle = GetDC(IntPtr.Zero);

        public void Dispose() => ReleaseDC(IntPtr.Zero, Handle);
    }
}
