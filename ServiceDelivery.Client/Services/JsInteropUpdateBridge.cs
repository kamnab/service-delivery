using Microsoft.JSInterop;

public static class JsInteropUpdateBridge
{
    private static UpdateNotifier? _notifier;

    public static void Init(UpdateNotifier notifier)
    {
        _notifier = notifier;
    }

    [JSInvokable]
    public static void NotifyUpdateAvailable()
    {
        _notifier?.Notify();
    }
}
