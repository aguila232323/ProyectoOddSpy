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
        string email = EmailEntry.Text?.Trim();
        string password = PasswordEntry.Text;
        string username = UsernameEntry.Text?.Trim();

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(username))
        {
            ErrorLabel.Text = "Por favor rellena todos los campos.";
            ErrorLabel.IsVisible = true;
            return;
        }

        // Verificar formato de correo electrónico
        if (!IsValidEmail(email))
        {
            ErrorLabel.Text = "Por favor introduce un correo electrónico válido.";
            ErrorLabel.IsVisible = true;
            return;
        }

        // Verificar contraseña: mínimo una letra, un número y un símbolo, mínimo 6 caracteres
        if (!IsValidPassword(password))
        {
            ErrorLabel.Text = "La contraseña debe tener al menos 6 caracteres, incluir letras, números y un símbolo.";
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
            HttpResponseMessage response = await _httpClient.PostAsync($"{ApiConfig.BaseUrl}/api/User", content);

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
    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
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
