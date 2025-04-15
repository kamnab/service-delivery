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
                await OnStatusChanged.Invoke();
        }
    }
}
