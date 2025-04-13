public class UpdateNotifier
{
    public event Action? OnUpdateAvailable;

    public void Notify() => OnUpdateAvailable?.Invoke();
}
