using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using App.Model;
using App.Model.DTO;
using Microsoft.Maui.Controls;

namespace App.View
{
    [QueryProperty(nameof(SelectedOffer), "selectedOffer")]
    public partial class OffersDetails : ContentPage
    {
        private readonly HttpClient _httpClient;
        private Offers _offer;

        public OffersDetails()
        {
            InitializeComponent();

            _httpClient = new HttpClient(new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            });
        }

        public string SelectedOffer
        {
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    var decoded = Uri.UnescapeDataString(value);
                    var offer = JsonSerializer.Deserialize<Offers>(decoded);
                    if (offer != null)
                    {
                        _offer = offer;
                        BindingContext = _offer;
                        _ = LoadAdditionalDataAsync();
                    }
                }
            }
        }

        private async Task LoadAdditionalDataAsync()
        {
            if (_offer == null)
                return;

            var userIdStr = Preferences.Get("UserId", null);
            if (Guid.TryParse(userIdStr, out var userId))
            {
                try
                {
                    var userOfferUrl = $"{ApiConfig.BaseUrl}/api/Offers/userOffer?userId={userId}&offerId={_offer.Id}";
                    var userOffer = await _httpClient.GetFromJsonAsync<UserOffer>(userOfferUrl);

                    if (userOffer != null)
                    {
                        _offer.LiberatedAmount = userOffer.LiberatedAmount;
                        _offer.MaxFreeAmount = userOffer.Offer.MaxFreeAmount;
                        _offer.IsRegistered = true;

                        System.Diagnostics.Debug.WriteLine($"Progreso real: {_offer.LiberatedAmount} / {_offer.MaxFreeAmount}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error cargando UserOffer: {ex.Message}");
                    _offer.IsRegistered = false;
                }

                // Verificar si el usuario está registrado (por si acaso)
                try
                {
                    var isRegisteredUrl = $"{ApiConfig.BaseUrl} /api/Offers/isRegistered?userId={userId}&offerId={_offer.Id}";
                    var response = await _httpClient.GetAsync(isRegisteredUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        _offer.IsRegistered = bool.Parse(json);
                    }
                    else
                    {
                        _offer.IsRegistered = false;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error verificando inscripción: {ex.Message}");
                    _offer.IsRegistered = false;
                }
            }
            else
            {
                _offer.IsRegistered = false;
            }

            UpdateProgressUI();
            UpdateButtons();
        }

        private void UpdateProgressUI()
        {
            double progress = _offer.MaxFreeAmount > 0
                ? (double)_offer.LiberatedAmount / _offer.MaxFreeAmount
                : 0;

            progressBar.Progress = progress;
            progressLabel.Text = $"{_offer.LiberatedAmount} / {_offer.MaxFreeAmount}";
        }

        private void UpdateButtons()
        {
            registerButton.IsVisible = !_offer.IsRegistered;
            alreadyRegisteredButton.IsVisible = _offer.IsRegistered;
        }

        private async void OnRegisterClicked(object sender, EventArgs e)
        {
            var userIdStr = Preferences.Get("UserId", null);
            if (!Guid.TryParse(userIdStr, out var userId))
            {
                await DisplayAlert("Error", "Debes iniciar sesión para inscribirte.", "OK");
                return;
            }

            var dto = new RegisterUserToOfferDTO
            {
                UserId = userId,
                OfferId = _offer.Id
            };

            var json = JsonSerializer.Serialize(dto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync($"{ApiConfig.BaseUrl}/api/Offers/register", content);
                if (response.IsSuccessStatusCode)
                {
                    await DisplayAlert("Éxito", "Te has inscrito correctamente en la oferta", "OK");
                    _offer.IsRegistered = true;
                    UpdateButtons();
                    await LoadAdditionalDataAsync();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    await DisplayAlert("Error", $"Error al inscribirte: {error}", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error al inscribirte: {ex.Message}", "OK");
            }
        }
    }
}
