namespace Pointframe.Services;

internal interface IGlobalHotkeyService : IDisposable
{
    event Action RegionSnipRequested;

    event Action WholeScreenSnipRequested;

    void Register();
}
