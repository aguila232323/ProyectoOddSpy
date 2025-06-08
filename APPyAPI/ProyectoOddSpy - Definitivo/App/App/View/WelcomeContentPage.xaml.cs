using App.Model;
using App.Model.DTO;
using App.Modelo;
using CommunityToolkit.Maui.Views;
using Newtonsoft.Json;
using SkiaSharp.Extended.UI.Controls;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace App.View;

public partial class WelcomeContentPage : ContentPage
{
    public int AnimationRepeatCount => -1;
    public SKLottieRepeatMode AnimationRepeatMode => SKLottieRepeatMode.Reverse;
    public string AnimationSource => "mafia.json";

    private readonly HttpClient _httpClient;
    public WelcomeContentPage()
	{
		InitializeComponent();

        HttpClientHandler insecureHandler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };

        _httpClient = new HttpClient(insecureHandler);
    }
    private async void OnRegisterTapped(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new Register());
    }
    private async void OnLoginButtonClicked(object sender, EventArgs e)
    {
        LoginOverlay.IsVisible = true;
        LoginOverlay.InputTransparent = false;
        
        await LoginPanel.TranslateTo(0, 0, 400, Easing.SinOut);
    }

    private async void OnOverlayTapped(object sender, TappedEventArgs e)
    {
        await LoginPanel.TranslateTo(0, 1000, 300, Easing.SinIn);
        LoginOverlay.IsVisible = false;
        LoginOverlay.InputTransparent = true;
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
            HttpResponseMessage response = await _httpClient.PostAsync($"{ApiConfig.BaseUrl}/api/User/login", content);

            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();

                // Parseamos el JSON para extraer user.id
                using var jsonDoc = JsonDocument.Parse(responseJson);

                var root = jsonDoc.RootElement;

                if (root.TryGetProperty("user", out JsonElement userElement) &&
                    userElement.TryGetProperty("id", out JsonElement idElement))
                {
                    string userIdString = idElement.GetString();

                    // Guardar userId en Preferences
                    Preferences.Set("UserId", userIdString);
                }

                ErrorLabel.IsVisible = false;

                await Shell.Current.ShowPopupAsync(new AlertaLogin(""));
                (Shell.Current as AppShell).SetAuthState(true);
                await Shell.Current.GoToAsync("//MainTabBar");
                EmailEntry.Text = "";
                PasswordEntry.Text = "";
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
