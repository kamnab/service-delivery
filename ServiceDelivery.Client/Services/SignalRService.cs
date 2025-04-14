// SignalRService.cs
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.SignalR.Client;

public class SignalRService : IAsyncDisposable
{
    private readonly IAccessTokenProvider _tokenProvider;
    private readonly NavigationManager _navigation;
    private HubConnection? _hubConnection;

    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;
    public string ConnectionStatus { get; private set; } = "Offline";
    public int ConnectionCount { get; private set; }

    public event Func<string, Task>? OnMessageReceived;
    public event Action? OnConnectionChanged; // 🔔 Notify UI
    public event Func<Task>? OnForceDisconnect;
    public event Action<int>? OnConnectionCountChanged;
    private readonly NetworkStatusService _networkStatus;
    public SignalRService(IAccessTokenProvider tokenProvider, NavigationManager navigation, NetworkStatusService networkStatus)
    {
        _tokenProvider = tokenProvider;
        _navigation = navigation;
        _networkStatus = networkStatus;
    }

    public async Task InitializeAsync()
    {
        if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
            return;

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(_navigation.ToAbsoluteUri("https://localhost:5001/notifications"), options =>
            {
                options.AccessTokenProvider = async () =>
                {
                    var result = await _tokenProvider.RequestAccessToken();
                    return result.TryGetToken(out var token) ? token.Value : string.Empty;
                };
            })
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.On<string>("ReceiveNotification", async message =>
        {
            if (OnMessageReceived is not null)
                await OnMessageReceived.Invoke(message);
        });

        _hubConnection.On("ForceDisconnect", async () =>
        {
            // if (OnForceDisconnect is not null)
            //     await OnForceDisconnect.Invoke();

            await _hubConnection.StopAsync();
        });

        _hubConnection.On<int>("ConnectionCountUpdated", count =>
        {
            Console.WriteLine($"[Client] ConnectionCountUpdated received: {count}");
            OnConnectionCountChanged?.Invoke(count);
        });

        _hubConnection.Reconnecting += error =>
        {
            UpdateStatus("Reconnecting...");
            return Task.CompletedTask;
        };

        _hubConnection.Reconnected += connectionId =>
        {
            UpdateStatus("Connected");
            _ = StartHeartbeatLoop(); // 🔁 Restart heartbeat loop after reconnect
            return Task.CompletedTask;
        };

        _hubConnection.Closed += async error =>
        {
            UpdateStatus("Offline");

            if (_networkStatus.IsOnline)
            {
                await RetryConnectIndefinitely();
            }
        };

        await RetryConnectIndefinitely(); // First connection attempt
        _ = StartHeartbeatLoop();         // 🔁 Start heartbeat loop after initial connect
    }

    private async Task RetryConnectIndefinitely()
    {
        while (_hubConnection?.State == HubConnectionState.Disconnected)
        {
            try
            {
                await _hubConnection.StartAsync();
                UpdateStatus("Connected");
                return;
            }
            catch
            {
                UpdateStatus("Retrying...");
                await Task.Delay(5000); // wait 5 seconds before retrying
            }
        }
    }

    private void UpdateStatus(string status)
    {
        ConnectionStatus = status;
        OnConnectionChanged?.Invoke(); // 🔔 Raise event to update UI
    }

    private async Task StartHeartbeatLoop()
    {
        while (_hubConnection?.State == HubConnectionState.Connected)
        {
            try
            {
                await _hubConnection.InvokeAsync("Heartbeat");
            }
            catch
            {
                // Log or handle failed heartbeat
            }

            await Task.Delay(TimeSpan.FromSeconds(10));
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection is not null)
        {
            await _hubConnection.StopAsync();
            await _hubConnection.DisposeAsync();
        }
    }

}


