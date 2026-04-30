using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using WpfApplication = System.Windows.Application;

namespace Pointframe.Services;

internal sealed class GlobalHotkeyService : IGlobalHotkeyService
{
    private readonly IUserSettingsService _userSettings;
    private readonly ILogger<GlobalHotkeyService> _logger;

    private NativeMethods.LowLevelKeyboardProc? _keyboardProc;
    private IntPtr _keyboardHook = IntPtr.Zero;
    private bool _disposed;

    public event Action? RegionSnipRequested;
    public event Action? WholeScreenSnipRequested;
    public event Action? WholeScreenRecordRequested;

    public GlobalHotkeyService(IUserSettingsService userSettings, ILogger<GlobalHotkeyService> logger)
    {
        _userSettings = userSettings;
        _logger = logger;
    }

    public void Register()
    {
        _keyboardProc = HookCallback;
        using var process = Process.GetCurrentProcess();
        var hMod = NativeMethods.GetModuleHandle(process.MainModule?.ModuleName);
        _keyboardHook = NativeMethods.SetWindowsHookEx(NativeMethods.WH_KEYBOARD_LL, _keyboardProc, hMod, 0);
        if (_keyboardHook == IntPtr.Zero)
        {
            _logger.LogWarning(
                "Failed to register low-level keyboard hook (error {Code}); Print Screen hotkey will not work",
                Marshal.GetLastWin32Error());
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam == (IntPtr)NativeMethods.WM_KEYDOWN)
        {
            var kb = Marshal.PtrToStructure<NativeMethods.KBDLLHOOKSTRUCT>(lParam);
            var shiftHeld = NativeMethods.GetAsyncKeyState(NativeMethods.VK_SHIFT) < 0;
            var ctrlHeld = NativeMethods.GetAsyncKeyState(NativeMethods.VK_CONTROL) < 0;

            var recordHotkey = _userSettings.Current.WholeScreenRecordHotkey;
            if (recordHotkey != 0 && kb.vkCode == recordHotkey && ctrlHeld)
            {
                WpfApplication.Current.Dispatcher.InvokeAsync(() => WholeScreenRecordRequested?.Invoke());
                return (IntPtr)1;
            }

            if (kb.vkCode == _userSettings.Current.RegionCaptureHotkey)
            {
                if (shiftHeld)
                {
                    WpfApplication.Current.Dispatcher.InvokeAsync(() => WholeScreenSnipRequested?.Invoke());
                }
                else
                {
                    WpfApplication.Current.Dispatcher.InvokeAsync(() => RegionSnipRequested?.Invoke());
                }

                return (IntPtr)1;
            }
        }

        return NativeMethods.CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_keyboardHook != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_keyboardHook);
            _keyboardHook = IntPtr.Zero;
        }
    }
}
