using App.Model;
using System.Text.Json;
using System.Collections.Generic;
using Microsoft.Maui.Controls;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Text;
using System.Threading.Tasks;
using App.Model.DTO;

namespace App.View;

[QueryProperty(nameof(SelectedMatchJson), "SelectedMatch")]
public partial class SureBetDetail : ContentPage, INotifyPropertyChanged
{
    private Matches _selectedMatch;
    private string selectedMatchJson;
    private bool _showWarning;
    private decimal _betAmount;
    private ObservableCollection<OfferViewModel> _availableOffers;
    private Guid _currentUserId;

    private decimal _betAmount1;
    private decimal _betAmountX;
    private decimal _betAmount2;
    decimal ganancia;

    public ObservableCollection<OfferViewModel> AvailableOffers
    {
        get => _availableOffers;
        set
        {
            _availableOffers = value;
            OnPropertyChanged(nameof(AvailableOffers));
        }
    }

    public string SelectedMatchJson
    {
        get => selectedMatchJson;
        set
        {
            selectedMatchJson = Uri.UnescapeDataString(value);
            SelectedMatch = JsonSerializer.Deserialize<Matches>(selectedMatchJson);
        }
    }

    public Matches SelectedMatch
    {
        get => _selectedMatch;
        set
        {
            _selectedMatch = value;
            OnPropertyChanged(nameof(SelectedMatch));
            OnPropertyChanged(nameof(Casino1Logo));
            OnPropertyChanged(nameof(CasinoXLogo));
            OnPropertyChanged(nameof(Casino2Logo));
            CheckCasinos();
            CalculateBetAmounts();
        }
    }

    public decimal BetAmount
    {
        get => _betAmount;
        set
        {
            _betAmount = value;
            OnPropertyChanged(nameof(BetAmount));
            CalculateBetAmounts();
        }
    }

    public decimal BetAmount1
    {
        get => _betAmount1;
        set
        {
            _betAmount1 = value;
            OnPropertyChanged(nameof(BetAmount1));
        }
    }

    public decimal BetAmountX
    {
        get => _betAmountX;
        set
        {
            _betAmountX = value;
            OnPropertyChanged(nameof(BetAmountX));
        }
    }

    public decimal BetAmount2
    {
        get => _betAmount2;
        set
        {
            _betAmount2 = value;
            OnPropertyChanged(nameof(BetAmount2));
        }
    }

    public bool ShowWarning
    {
        get => _showWarning;
        set
        {
            _showWarning = value;
            OnPropertyChanged(nameof(ShowWarning));
        }
    }

    public string WarningMessage => "¡Advertencia! Necesitas más de 1 cuenta para hacer esta apuesta";

    private static readonly Dictionary<string, string> CasinoLogoMap = new()
    {
        { "Interwetten", "logo_interwetten.png" },
        { "Bwin", "logo_bwin.png" },
        { "Betfair", "logo_betfair.png" },
        { "ApuestasAndalucia", "logo_apuestasandalucia.png" },
    };

    public string Casino1Logo => GetLogoPath(SelectedMatch?.Casino1);
    public string CasinoXLogo => GetLogoPath(SelectedMatch?.CasinoX);
    public string Casino2Logo => GetLogoPath(SelectedMatch?.Casino2);

    private string GetLogoPath(string casinoName)
    {
        if (string.IsNullOrWhiteSpace(casinoName))
            return "logo_betfair.png";

        return CasinoLogoMap.TryGetValue(casinoName.Trim(), out var logo)
            ? logo
            : "logo_betfair.png";
    }

    private void CheckCasinos()
    {
        if (SelectedMatch == null) return;

        var casino1 = SelectedMatch.Casino1?.Trim();
        var casinoX = SelectedMatch.CasinoX?.Trim();
        var casino2 = SelectedMatch.Casino2?.Trim();

        ShowWarning = !string.IsNullOrWhiteSpace(casino1) &&
                      !string.IsNullOrWhiteSpace(casinoX) &&
                      !string.IsNullOrWhiteSpace(casino2) &&
                      casino1 == casinoX && casinoX == casino2;

        
        UpdateAvailableOffers(casino1, casinoX, casino2);
    }

    private async void UpdateAvailableOffers(string casino1, string casinoX, string casino2)
    {
        try
        {
            if (_currentUserId == Guid.Empty)
            {
                await DisplayAlert("Error", "Debes iniciar sesión para ver las ofertas", "OK");
                return;
            }

            var casinos = new HashSet<string> { casino1, casinoX, casino2 }
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct()
                .ToList();

            System.Diagnostics.Debug.WriteLine($"Casinos recibidos: {string.Join(", ", casinos)}");

            if (!casinos.Any())
            {
                AvailableOffers = new ObservableCollection<OfferViewModel>();
                return;
            }

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            using var client = new HttpClient(handler);
            client.Timeout = TimeSpan.FromSeconds(10);

            // URL de la API
            var apiUrl = $"{ApiConfig.BaseUrl}/api/Offers";
            System.Diagnostics.Debug.WriteLine($"Intentando conectar a: {apiUrl}");

            try
            {
                var response = await client.GetAsync(apiUrl);
                System.Diagnostics.Debug.WriteLine($"Respuesta del servidor: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"Error del servidor: {errorContent}");
                    throw new Exception($"Error al cargar ofertas: {response.StatusCode} - {errorContent}");
                }

                var content = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Contenido recibido: {content}");

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var allOffers = JsonSerializer.Deserialize<List<Offers>>(content, options);

                if (allOffers == null || !allOffers.Any())
                {
                    System.Diagnostics.Debug.WriteLine("No se encontraron ofertas");
                    AvailableOffers = new ObservableCollection<OfferViewModel>();
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"Número de ofertas encontradas: {allOffers.Count}");
                foreach (var offer in allOffers)
                {
                    System.Diagnostics.Debug.WriteLine($"Oferta en BD: Casino='{offer.Casino}', Title='{offer.Title}'");
                }

                var filteredOffers = allOffers
                    .Where(o => casinos.Any(c => c.Equals(o.Casino, StringComparison.OrdinalIgnoreCase)))
                    .Select(o => new OfferViewModel
                    {
                        Id = o.Id,
                        Title = o.Title,
                        Type = o.Type,
                        CasinoLogo = GetLogoPath(o.Casino),
                        IsSelected = false
                    })
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"Número de ofertas filtradas: {filteredOffers.Count}");
                foreach (var offer in filteredOffers)
                {
                    System.Diagnostics.Debug.WriteLine($"Oferta filtrada: {offer.Title}");
                }

                // Verificar suscripciones para cada oferta
                foreach (var offer in filteredOffers)
                {
                    try
                    {
                        var isRegisteredUrl = $"{ApiConfig.BaseUrl}/api/Offers/isRegistered?userId={_currentUserId}&offerId={offer.Id}";
                        System.Diagnostics.Debug.WriteLine($"Verificando suscripción: {isRegisteredUrl}");

                        var isRegisteredResponse = await client.GetAsync(isRegisteredUrl);
                        if (isRegisteredResponse.IsSuccessStatusCode)
                        {
                            var isRegistered = await isRegisteredResponse.Content.ReadAsStringAsync();
                            offer.IsRegistered = bool.Parse(isRegistered);
                            System.Diagnostics.Debug.WriteLine($"Estado de suscripción para {offer.Title}: {offer.IsRegistered}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"Error al verificar suscripción: {isRegisteredResponse.StatusCode}");
                            offer.IsRegistered = false;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error al verificar suscripción para {offer.Title}: {ex.Message}");
                        offer.IsRegistered = false;
                    }
                }

                AvailableOffers = new ObservableCollection<OfferViewModel>(filteredOffers);
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error de conexión HTTP: {ex.Message}");
                await DisplayAlert("Error de conexión", "No se pudo conectar al servidor. Verifica tu conexión a internet.", "OK");
                AvailableOffers = new ObservableCollection<OfferViewModel>();
            }
            catch (TaskCanceledException)
            {
                System.Diagnostics.Debug.WriteLine("La solicitud se agotó por tiempo de espera");
                await DisplayAlert("Error de tiempo de espera", "La conexión al servidor tardó demasiado. Por favor, intenta de nuevo.", "OK");
                AvailableOffers = new ObservableCollection<OfferViewModel>();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error en UpdateAvailableOffers: {ex}");
            await DisplayAlert("Error", "No se pudieron cargar las ofertas. Por favor, intenta de nuevo más tarde.", "OK");
            AvailableOffers = new ObservableCollection<OfferViewModel>();
        }
    }

    private async void OnOfferSelected(object sender, EventArgs e)
    {
        var selectedOffer = (sender as CheckBox)?.BindingContext as OfferViewModel;
        if (selectedOffer == null) return;

        if (selectedOffer.IsSelected && !selectedOffer.IsRegistered)
        {
            var result = await DisplayAlert("Suscripción requerida",
                "Para usar esta oferta necesitas suscribirte. ¿Deseas suscribirte ahora?",
                "Sí", "No");

            if (result)
            {
                await RegisterToOffer(selectedOffer.Id);
            }
            else
            {
                selectedOffer.IsSelected = false;
            }
        }
    }

    private async Task RegisterToOffer(Guid offerId)
    {
        try
        {
            var dto = new RegisterUserToOfferDTO
            {
                UserId = _currentUserId,
                OfferId = offerId
            };

            var json = JsonSerializer.Serialize(dto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var client = new HttpClient();
            var response = await client.PostAsync($"{ApiConfig.BaseUrl}/api/Offers/register", content);

            if (response.IsSuccessStatusCode)
            {
                await DisplayAlert("Éxito", "Te has suscrito correctamente a la oferta", "OK");
                var offer = AvailableOffers.FirstOrDefault(o => o.Id == offerId);
                if (offer != null)
                {
                    offer.IsRegistered = true;
                }
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                await DisplayAlert("Error", $"Error al suscribirte: {error}", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error: {ex.Message}", "OK");
        }
    }

    private void CalculateBetAmounts()
    {
        if (SelectedMatch == null || BetAmount <= 0) return;

        if (!decimal.TryParse(SelectedMatch.Odds1, out var odds1) ||
            !decimal.TryParse(SelectedMatch.OddsX, out var oddsX) ||
            !decimal.TryParse(SelectedMatch.Odds2, out var odds2))
        {
            BetAmount1 = BetAmountX = BetAmount2 = 0;
            OnPropertyChanged(nameof(BetAmount1));
            OnPropertyChanged(nameof(BetAmountX));
            OnPropertyChanged(nameof(BetAmount2));
            return;
        }

        var p1 = 1m / odds1;
        var px = 1m / oddsX;
        var p2 = 1m / odds2;

        var totalProb = p1 + px + p2;

        BetAmount1 = BetAmount * (p1 / totalProb);
        BetAmountX = BetAmount * (px / totalProb);
        BetAmount2 = BetAmount * (p2 / totalProb);

        ganancia = (BetAmount1 * odds1) - BetAmount;

        OnPropertyChanged(nameof(BetAmount1));
        OnPropertyChanged(nameof(BetAmountX));
        OnPropertyChanged(nameof(BetAmount2));
    }

    private void OnWarningAcknowledged(object sender, EventArgs e)
    {
        ShowWarning = false;
    }

    private async void OnAcceptBet(object sender, EventArgs e)
    {
        if (BetAmount <= 0)
        {
            await DisplayAlert("Error", "Por favor, introduce una cantidad válida", "OK");
            return;
        }

        var selectedOffer = AvailableOffers.FirstOrDefault(o => o.IsSelected && o.IsRegistered);
        if (selectedOffer == null)
        {
            await DisplayAlert("Error", "Debes seleccionar una oferta registrada", "OK");
            return;
        }

        try
        {
            using var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };
            using var client = new HttpClient(handler);

            //  Obtener userOfferId desde la API
            var userOfferIdUrl = $"{ApiConfig.BaseUrl}/api/Offers/getUserOfferId?userId={_currentUserId}&offerId={selectedOffer.Id}";
            var userOfferResponse = await client.GetAsync(userOfferIdUrl);

            if (!userOfferResponse.IsSuccessStatusCode)
            {
                await DisplayAlert("Error", "No se pudo obtener el UserOfferId", "OK");
                return;
            }

            var userOfferIdString = await userOfferResponse.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"Respuesta userOfferId: '{userOfferIdString}'");

            // Quita las comillas dobles que vienen en el JSON string
            userOfferIdString = userOfferIdString.Trim('"');

            if (!Guid.TryParse(userOfferIdString, out var userOfferId))
            {
                await DisplayAlert("Error", "El UserOfferId recibido no es válido", "OK");
                return;
            }


            // Preparar DTO con UserOfferId
            var registerBetDto = new
            {
                UserOfferId = userOfferId,
                Amount = BetAmount,
                HomeTeam = SelectedMatch.HomeTeam,
                HomeTeamImg = SelectedMatch.HomeTeamImg,
                AwayTeam = SelectedMatch.AwayTeam,
                AwayTeamImg = SelectedMatch.AwayTeamImg,
                Casino1 = SelectedMatch.Casino1,
                AmountCasino1 = BetAmount1,
                Odds1 = SelectedMatch.Odds1,
                CasinoX = SelectedMatch.CasinoX,
                AmountCasinoX = BetAmountX,
                OddsX = SelectedMatch.OddsX,
                Casino2 = SelectedMatch.Casino2,
                AmountCasino2 = BetAmount2,
                Odds2 = SelectedMatch.Odds2
            };

            var json = JsonSerializer.Serialize(registerBetDto);
            var postContent = new StringContent(json, Encoding.UTF8, "application/json");

            // Registrar la apuesta
            
            var registerBetResponse = await client.PostAsync($"{ApiConfig.BaseUrl}/api/Offers/registerBet", postContent);

            if (registerBetResponse.IsSuccessStatusCode)
            {
                string mensaje = ganancia >= 0
                    ? $"Ganancias: {ganancia:C}"
                    : $"Pérdidas: {ganancia:C}";

                await DisplayAlert("Apuesta registrada", $"Has apostado {BetAmount:C}\n{mensaje}", "OK");
            }
            else
            {
                var error = await registerBetResponse.Content.ReadAsStringAsync();
                await DisplayAlert("Error", $"Error al registrar la apuesta: {error}", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error: {ex.Message}", "OK");
        }
    }



    public SureBetDetail()
    {
        InitializeComponent();
        BindingContext = this;
        AvailableOffers = new ObservableCollection<OfferViewModel>();


        var userIdString = Preferences.Get("UserId", null);
        if (Guid.TryParse(userIdString, out var userId))
        {
            _currentUserId = userId;
        }
    }
    public new event PropertyChangedEventHandler PropertyChanged;

    protected new void OnPropertyChanged(string propertyName)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public class OfferViewModel : INotifyPropertyChanged
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string Type { get; set; }
    public string CasinoLogo { get; set; }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            _isSelected = value;
            OnPropertyChanged(nameof(IsSelected));
        }
    }

    private bool _isRegistered;
    public bool IsRegistered
    {
        get => _isRegistered;
        set
        {
            _isRegistered = value;
            OnPropertyChanged(nameof(IsRegistered));
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
