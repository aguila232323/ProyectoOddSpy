using App.Model;
using CommunityToolkit.Maui.Markup;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Windows.Input;

namespace App.View;

public partial class BetsComparerContentPage : ContentPage
{
    private readonly ObservableCollection<Matches> _matches = new();
    private CancellationTokenSource _wsCancellation;
    private ClientWebSocket _webSocket;
    private bool _isConnected = false;
    private bool _isReconnecting = false;
    private const int RECONNECT_DELAY = 3000;
    private const string WS_URL = "wss://oddspy.store";

    public ICommand ItemTappedCommand { get; private set; }

    public BetsComparerContentPage()
    {
        InitializeComponent();
        InitializeCommands();
        collectionView.ItemsSource = _matches;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        loadingAnimation.IsVisible = true;
        collectionView.IsVisible = false;

        if (!_isConnected && !_isReconnecting)
        {
            await InitializeWebSocketConnection();
        }
    }


    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        Debug.WriteLine("OnDisappearing called");
        await CleanupWebSocket();
    }

    private void InitializeCommands()
    {
        ItemTappedCommand = new Command<Matches>(async selectedMatch =>
        {
            if (selectedMatch != null)
            {
                Shell.SetTabBarIsVisible(this, true);
                var json = Uri.EscapeDataString(JsonSerializer.Serialize(selectedMatch));
                await Shell.Current.GoToAsync($"{nameof(ComparerDetail)}?SelectedMatch={json}");
            }
        });
    }

    private async Task InitializeWebSocketConnection()
    {
        if (_isConnected || _isReconnecting)
            return;

        _isReconnecting = true;
        

        try
        {
            _wsCancellation?.Cancel();
            _wsCancellation?.Dispose();
            _wsCancellation = new CancellationTokenSource();

            _webSocket?.Dispose();
            _webSocket = new ClientWebSocket();

            Debug.WriteLine("Connecting to WebSocket...");
            await ConnectToWebSocket();
        }
        finally
        {
            _isReconnecting = false;
        }
    }

    private async Task CleanupWebSocket()
    {
        try
        {
            _wsCancellation?.Cancel();

            if (_webSocket?.State == WebSocketState.Open || _webSocket?.State == WebSocketState.Connecting)
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Page navigation", CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Cleanup error: {ex.Message}");
        }
        finally
        {
            _webSocket?.Dispose();
            _webSocket = null;

            _wsCancellation?.Dispose();
            _wsCancellation = null;

            _isConnected = false;
        }
    }

    private async Task ConnectToWebSocket()
    {
        while (_wsCancellation != null && !_wsCancellation.IsCancellationRequested)
        {
            try
            {
                await _webSocket.ConnectAsync(new Uri(WS_URL), _wsCancellation.Token);
                _isConnected = true;
                Debug.WriteLine("WebSocket connected");

                await SendInitialMessage();
                await ProcessWebSocketMessages();
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("WebSocket operation canceled");
                break;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"WebSocket error: {ex.Message}");
                _isConnected = false;

                if (_wsCancellation != null && !_wsCancellation.IsCancellationRequested)
                {
                    Debug.WriteLine($"Reconnecting in {RECONNECT_DELAY}ms...");
                    await Task.Delay(RECONNECT_DELAY);

                    _webSocket?.Dispose();
                    _webSocket = new ClientWebSocket();
                }
            }
        }
    }

    private async Task SendInitialMessage()
    {
        if (_wsCancellation == null || _wsCancellation.IsCancellationRequested)
            return;

        var initMessage = Encoding.UTF8.GetBytes("{\"rol\":\"receptor\", \"tipo\":\"comparaciones\"}");
        await _webSocket.SendAsync(
            new ArraySegment<byte>(initMessage),
            WebSocketMessageType.Text,
            true,
            _wsCancellation.Token);
    }

    private async Task ProcessWebSocketMessages()
    {
        var buffer = new byte[100_000];

        while (_webSocket.State == WebSocketState.Open &&
               _wsCancellation != null && !_wsCancellation.IsCancellationRequested)
        {
            try
            {
                var result = await _webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    _wsCancellation.Token);

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    ProcessIncomingData(json);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Message processing error: {ex.Message}");

                if (_wsCancellation != null && !_wsCancellation.IsCancellationRequested)
                {
                    throw;
                }
            }
            finally
            {
                loadingAnimation.IsVisible = false;
                collectionView.IsVisible = true;
            }
        }
    }

    private void ProcessIncomingData(string json)
    {
        try
        {
            var newMatches = JsonSerializer.Deserialize<List<Matches>>(json);
            UpdateMatchesCollection(newMatches);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Data processing error: {ex.Message}");
        }
    }

    private void UpdateMatchesCollection(List<Matches> newMatches)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            for (int i = _matches.Count - 1; i >= 0; i--)
            {
                if (!newMatches.Any(m =>
                    m.HomeTeam == _matches[i].HomeTeam &&
                    m.AwayTeam == _matches[i].AwayTeam))
                {
                    _matches.RemoveAt(i);
                }
            }

            foreach (var newMatch in newMatches)
            {
                var existing = _matches.FirstOrDefault(m =>
                    m.HomeTeam == newMatch.HomeTeam &&
                    m.AwayTeam == newMatch.AwayTeam);

                if (existing == null)
                {
                    _matches.Add(newMatch);
                }
                else if (!existing.EsIgualA(newMatch))
                {
                    _matches[_matches.IndexOf(existing)] = newMatch;
                }
            }
        });
    }



    private void OnSearchBarTextChanged(object sender, TextChangedEventArgs e)
    {
        var searchText = e.NewTextValue?.ToLower() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(searchText))
        {
            collectionView.ItemsSource = _matches;
            return;
        }

        var filtered = _matches.Where(match =>
            match.HomeTeam?.ToLower().Contains(searchText) == true ||
            match.AwayTeam?.ToLower().Contains(searchText) == true).ToList();

        collectionView.ItemsSource = filtered;
    }
}
