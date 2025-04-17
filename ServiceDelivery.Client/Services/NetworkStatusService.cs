public class NetworkStatusService
{
    public bool IsOnline { get; private set; } = true;

    public event Func<Task>? OnStatusChanged;

    public async Task SetOnlineStatus(bool isOnline)
    {
        if (IsOnline != isOnline)
        {
            IsOnline = isOnline;
            if (OnStatusChanged != null)
                await OnStatusChanged.Invoke();  // Notify the subscriber of the change
        }
    }

    // Toast event triggered by external components
    public event Func<Task>? OnShowToast;

    public async Task ShowToast()
    {
        if (OnShowToast != null)
        {
            await OnShowToast.Invoke();  // Trigger toast display from external event
        }
    }
}
