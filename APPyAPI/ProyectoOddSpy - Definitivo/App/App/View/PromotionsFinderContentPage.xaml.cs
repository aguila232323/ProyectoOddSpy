using System.Collections.ObjectModel;
using System.Text.Json;
using System.Reflection;
using App.Model;
using System.Windows.Input;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Http;

namespace App.View;

public partial class PromotionsFinderContentPage : ContentPage
{
    public ICommand ItemTappedCommand { get; private set; }

    HttpClientHandler insecureHandler = new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
    };

    private readonly HttpClient _httpClient;

    public PromotionsFinderContentPage()
    {
        InitializeComponent();
        InitializeCommands();
        BindingContext = this;

        _httpClient = new HttpClient(insecureHandler);
        loadingAnimation.IsVisible = true;
        collectionView.IsVisible = false;

        LoadDataFromApi();
    }



    private void InitializeCommands()
    {
        ItemTappedCommand = new Command<Offers>(async selectedOffer =>
        {
            if (selectedOffer != null)
            {
                var userIdString = Preferences.Get("UserId", null);
                if (Guid.TryParse(userIdString, out var userId))
                {
                    var url = $"{ApiConfig.BaseUrl}/api/Offers/isRegistered?userId={userId}&offerId={selectedOffer.Id}";

                    try
                    {
                        using var client = new HttpClient(new HttpClientHandler
                        {
                            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                        });
                        var response = await client.GetAsync(url);

                        if (response.IsSuccessStatusCode)
                        {
                            var result = await response.Content.ReadAsStringAsync();
                            selectedOffer.IsRegistered = bool.Parse(result);
                        }
                        else
                        {
                            selectedOffer.IsRegistered = false;
                        }
                    }
                    catch
                    {
                        selectedOffer.IsRegistered = false;
                    }
                }
                var json = Uri.EscapeDataString(JsonSerializer.Serialize(selectedOffer));
                await Shell.Current.GoToAsync($"{nameof(OffersDetails)}?selectedOffer={json}");
            }
        });

    }

    private async void LoadDataFromApi()
    {
        try
        {

            string url = $"{ApiConfig.BaseUrl}/api/Offers";

            var offers = await _httpClient.GetFromJsonAsync<List<Offers>>(url);

            if (offers != null)
            {
                loadingAnimation.IsVisible = false;
                collectionView.IsVisible = true;
                collectionView.ItemsSource = offers;
            }
            else
            {
                await DisplayAlert("Error", "No se encontraron ofertas", "OK");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ERROR al llamar a la API: {ex.Message}");
            await DisplayAlert("Error", $"Error: {ex}", "OK");
        }

    }
}
