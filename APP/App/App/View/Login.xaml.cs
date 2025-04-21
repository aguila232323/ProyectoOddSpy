using System.Net.Http;
using System.Text;
using App.Modelo;
using Microsoft.Maui.Controls;
using Newtonsoft.Json;

namespace App.View
{
    public partial class Login : ContentPage
    {
        private readonly HttpClient _httpClient;

        public Login()
        {
            InitializeComponent();

            HttpClientHandler insecureHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            _httpClient = new HttpClient(insecureHandler);
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            string email = EmailEntry.Text;
            string password = PasswordEntry.Text;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ErrorLabel.Text = "Por favor ingresa usuario y contraseña.";
                ErrorLabel.IsVisible = true;
                return;
            }

            var loginData = new LoginDto
            {
                Email = email,
                Password = password
            };

            var json = JsonConvert.SerializeObject(loginData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                HttpResponseMessage response = await _httpClient.PostAsync("http://10.0.2.2:5016/api/User/login", content);

                if (response.IsSuccessStatusCode)
                {
                    ErrorLabel.IsVisible = false;

                    await DisplayAlert("Login Exitoso", "Bienvenido, " + email, "OK");
                    (Shell.Current as AppShell).SetAuthState(true);
                    await Shell.Current.GoToAsync("//MainTabBar");
                }
                else
                {
                    ErrorLabel.Text = "Usuario o contraseña incorrectos.";
                    ErrorLabel.IsVisible = true;
                }
            }
            catch (Exception ex)
            {
                ErrorLabel.Text = $"Error: {ex.GetType().Name} - {ex.Message}";
                ErrorLabel.IsVisible = true;
            }
        }
    }
}
