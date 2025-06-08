using App.Model;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Windows.Input;

namespace App.View;

public partial class SureBetsContentPage : ContentPage
{

    public enum MatchFilterType { None, Surebets, BonusLiberators }
    public enum BenefitOrder { Lower, Higher }

    private MatchFilterType _selectedFilter = MatchFilterType.None;
    private BenefitOrder _selectedBenefitOrder;
    private readonly ObservableCollection<Matches> _matches = new();
    private readonly ObservableCollection<Matches> _filteredMatches = new();
    private string selectedFilter = "Todos";
    private string selectedBenefitOrder = "None";
    private CancellationTokenSource _wsCancellation;
    private ClientWebSocket _webSocket;
    private bool _isConnected = false;
    private bool _isReconnecting = false;
    private const int RECONNECT_DELAY = 3000;
    private const string WS_URL = "wss://oddspy.store";


    public ICommand ItemTappedCommand { get; private set; }

    public SureBetsContentPage()
    {
        InitializeComponent();
        InitializeCommands();
        collectionView.ItemsSource = _filteredMatches;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        Debug.WriteLine("OnAppearing called");

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
                await Shell.Current.GoToAsync($"{nameof(SureBetDetail)}?SelectedMatch={json}");
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
                await _webSocket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Page navigation",
                    CancellationToken.None);
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

        var initMessage = Encoding.UTF8.GetBytes("{\"rol\":\"receptor\", \"tipo\":\"surebets\"}");
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
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by server", CancellationToken.None);
                    _isConnected = false;
                    break;
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Message processing error: {ex.Message}");
                _isConnected = false;
                break;
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
        //Deserializa el json y lo convierte a los objetos de Matches
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
            //Borra partidos desactualizados
            for (int i = _matches.Count - 1; i >= 0; i--)
            {
                if (!newMatches.Any(m =>
                    m.HomeTeam == _matches[i].HomeTeam &&
                    m.AwayTeam == _matches[i].AwayTeam))
                {
                    _matches.RemoveAt(i);
                }
            }

            //Actuliza con los partidos nuevos
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

            ApplyFilters();
        });
    }

    //Funcionalidad de los chips
    private void OnChipClicked(object sender, EventArgs e)
    {
        
        ResetChipStyles();

        
        var selectedChip = (Button)sender;
        selectedChip.Style = (Style)Application.Current.Resources["ChipButtonSelectedStyle"];


        if (sender == SurebetsChip)
            _selectedFilter = MatchFilterType.Surebets;
        else if (sender == BonnusLiberatorsChip)
            _selectedFilter = MatchFilterType.BonusLiberators;
        else if (sender == LowerChip)
            _selectedBenefitOrder = BenefitOrder.Lower;
        else if (sender == HigherChip)
            _selectedBenefitOrder = BenefitOrder.Higher;

        ApplyFilters();
    }

    private void ResetChipStyles()
    {
        SurebetsChip.Style = (Style)Application.Current.Resources["ChipButtonStyle"];
        BonnusLiberatorsChip.Style = (Style)Application.Current.Resources["ChipButtonStyle"];
        LowerChip.Style = (Style)Application.Current.Resources["ChipButtonStyle"];
        HigherChip.Style = (Style)Application.Current.Resources["ChipButtonStyle"];
    }

    private void ApplyFilters()
    {
        var filtered = _matches.AsEnumerable();

        // Filtrado por tipo de apuesta
        filtered = _selectedFilter switch
        {
            MatchFilterType.Surebets => filtered.Where(m => m.Type.StartsWith("Surebet", StringComparison.OrdinalIgnoreCase)),
            MatchFilterType.BonusLiberators => filtered.Where(m => m.Type.StartsWith("BonusLiberator", StringComparison.OrdinalIgnoreCase)),
            _ => filtered
        };

        // Ordenamos los partidos
        filtered = _selectedBenefitOrder switch
        {
            BenefitOrder.Lower => filtered.OrderBy(m => m.BenefitPecentaje),
            BenefitOrder.Higher => filtered.OrderByDescending(m => m.BenefitPecentaje),
            _ => filtered
        };

        // Actualizar Interfaz
        _filteredMatches.Clear();
        foreach (var item in filtered)
            _filteredMatches.Add(item);
    }
}