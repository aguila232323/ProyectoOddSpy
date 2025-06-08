using App.Model;
using App.Modelo;
using Newtonsoft.Json;
using System.Net.Http;

namespace App.View
{
    public partial class ProfilePage : ContentPage
    {
        private readonly HttpClient _httpClient;

        public ProfilePage()
        {
            InitializeComponent();

            HttpClientHandler insecureHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };
            _httpClient = new HttpClient(insecureHandler);

            LoadUserData();
        }

        private async void LoadUserData()
        {
            // Obtén el userId de Preferences
            string userId = Preferences.Get("UserId", string.Empty);

            if (string.IsNullOrEmpty(userId))
            {
                await DisplayAlert("Error", "No se encontró el ID del usuario.", "OK");
                return;
            }

            try
            {
                // Llama a tu API con el userId
                string url = $"{ApiConfig.BaseUrl}/api/User/{userId}";
                HttpResponseMessage response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    var user = JsonConvert.DeserializeObject<User>(json);

                    // Muestra los datos en los labels
                    UserNameLabel.Text = user.Username;
                    UserEmailLabel.Text = user.Email;
                    UserFreeBetsLabel.Text = user.FreeBets.ToString();
                }
                else
                {
                    await DisplayAlert("Error", "No se pudieron obtener los datos del usuario.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error: {ex.Message}", "OK");
            }
        }


        private async void OnEditProfileClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync($"EditProfilePage?name={UserNameLabel.Text}&email={UserEmailLabel.Text}");
        }

        private async void OnChangePasswordClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//ChangePasswordPage");
        }
        private async void OnHistoryBetsClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//HistoryBet");
        }

        private async void CerrarSesion_Clicked(object sender, EventArgs e)
        {
            // Lógica para cerrar sesión
            await DisplayAlert("Cerrar Sesión", "Sesión cerrada correctamente.", "OK");
            (Shell.Current as AppShell).SetAuthState(false);
            await Shell.Current.GoToAsync("//WelcomeContentPage");
        }
    }
}
