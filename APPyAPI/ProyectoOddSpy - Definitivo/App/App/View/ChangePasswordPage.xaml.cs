using App.Model;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;

namespace App.View;

public partial class ChangePasswordPage : ContentPage
{
    private readonly HttpClient _httpClient;

    public ChangePasswordPage()
    {
        InitializeComponent();

        HttpClientHandler insecureHandler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };
        _httpClient = new HttpClient(insecureHandler);
    }

    private async void OnChangePasswordClicked(object sender, EventArgs e)
    {
        string userId = Preferences.Get("UserId", string.Empty);
        string currentPassword = CurrentPasswordEntry.Text;
        string newPassword = NewPasswordEntry.Text;

        if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
        {
            ErrorLabel.Text = "Por favor completa todos los campos.";
            ErrorLabel.IsVisible = true;
            return;
        }

        if (!IsValidPassword(newPassword))
        {
            ErrorLabel.Text = "La nueva contraseña debe tener al menos 6 caracteres, incluir letras, números y un símbolo.";
            ErrorLabel.IsVisible = true;
            return;
        }

        var changePasswordDTO = new
        {
            CurrentPassword = currentPassword,
            NewPassword = newPassword
        };

        var json = JsonConvert.SerializeObject(changePasswordDTO);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PutAsync($"{ApiConfig.BaseUrl}/api/User/{userId}/change-password", content);

        if (response.IsSuccessStatusCode)
        {
            ConfirmationOverlay.IsVisible = true;
            await Navigation.PopAsync();
        }
        else
        {
            ErrorLabel.Text = "No se pudo cambiar la contraseña.";
            ErrorLabel.IsVisible = true;
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//ProfilePage");
    }


    private bool IsValidPassword(string password)
    {
        if (password.Length < 6)
            return false;

        bool hasLetter = false, hasDigit = false, hasSymbol = false;

        foreach (char c in password)
        {
            if (char.IsLetter(c))
                hasLetter = true;
            else if (char.IsDigit(c))
                hasDigit = true;
            else if (!char.IsWhiteSpace(c))
                hasSymbol = true;
        }

        return hasLetter && hasDigit && hasSymbol;
    }

}
