using App.Model;
using App.Modelo;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;

namespace App.View;

public partial class Register : ContentPage
{
    private readonly HttpClient _httpClient;

    public Register()
    {
        InitializeComponent();

        HttpClientHandler insecureHandler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };
        _httpClient = new HttpClient(insecureHandler);
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        string email = EmailEntry.Text;
        string password = PasswordEntry.Text;
        string username = UsernameEntry.Text;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(username))
        {
            ErrorLabel.Text = "Por favor rellena todos los campos";
            ErrorLabel.IsVisible = true;
            return;
        }

        var registerData = new User
        {
            Email = email,
            Username = username,
            Password = password
        };

        var json = JsonConvert.SerializeObject(registerData);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            HttpResponseMessage response = await _httpClient.PostAsync("http://10.0.2.2:5016/api/User", content);

            if (response.IsSuccessStatusCode)
            {
                ErrorLabel.IsVisible = false;

                await DisplayAlert("Registro Exitoso", "Bienvenido, " + email, "OK");
                await Navigation.PushAsync(new WelcomeContentPage());
            }
            else
            {
                string errorResponse = await response.Content.ReadAsStringAsync();
                ErrorLabel.Text = "Error al registrar el usuario.";
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
