using App.Model;
using Newtonsoft.Json;
using System.Collections.ObjectModel;

using System.Text;
namespace App.View;

public partial class HistoryBet : ContentPage
{
    private readonly HttpClient _httpClient;
    public ObservableCollection<Bets> Bets { get; set; } = new();

    public HistoryBet()
    {
        InitializeComponent();

        HttpClientHandler insecureHandler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };

        _httpClient = new HttpClient(insecureHandler);

        BindingContext = this;

        LoadUserBets();
    }
    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
    private async void LoadUserBets()
    {
        try
        {
            string userId = Preferences.Get("UserId", string.Empty);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return;
            }

            string url = $"{ApiConfig.BaseUrl}/api/Offers/userBets/{userId}";
            HttpResponseMessage response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var betsList = JsonConvert.DeserializeObject<List<Bets>>(json);

                Bets.Clear();

                foreach (var bet in betsList)
                {
                    Bets.Add(bet);
                }
            }
            else
            {
            }
        }
        catch (Exception ex)
        {

        }
    }
}
