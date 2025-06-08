using App.Model;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Web;

namespace App.View;

[QueryProperty(nameof(CurrentUsername), "username")]
[QueryProperty(nameof(CurrentEmail), "email")]
public partial class EditProfilePage : ContentPage
{
    private readonly HttpClient _httpClient;

    private string _currentUsername;
    private string _currentEmail;

    public string CurrentUsername
    {
        get => _currentUsername;
        set => _currentUsername = Uri.UnescapeDataString(value ?? string.Empty);
    }

    public string CurrentEmail
    {
        get => _currentEmail;
        set => _currentEmail = Uri.UnescapeDataString(value ?? string.Empty);
    }

    public EditProfilePage()
    {
        InitializeComponent();

        // HttpClient con SSL inseguro temporal
        HttpClientHandler insecureHandler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };
        _httpClient = new HttpClient(insecureHandler);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Asignar valores a los campos cuando la página aparece
        UsernameEntry.Text = CurrentUsername;
        EmailEntry.Text = CurrentEmail;
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        string userId = Preferences.Get("UserId", string.Empty);
        string newUsername = UsernameEntry.Text?.Trim();
        string newEmail = EmailEntry.Text?.Trim();

        if (string.IsNullOrWhiteSpace(newUsername) || string.IsNullOrWhiteSpace(newEmail))
        {
            ErrorLabel.Text = "Por favor completa todos los campos.";
            ErrorLabel.IsVisible = true;
            return;
        }

        // Validar email
        if (!IsValidEmail(newEmail))
        {
            ErrorLabel.Text = "Introduce un correo electrónico válido.";
            ErrorLabel.IsVisible = true;
            return;
        }

        var updateUserDTO = new
        {
            Username = newUsername,
            Email = newEmail
        };

        var json = JsonConvert.SerializeObject(updateUserDTO);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PutAsync($"{ApiConfig.BaseUrl} /api/User/erId", content);

        if (response.IsSuccessStatusCode)
        {
            await DisplayAlert("Éxito", "Perfil actualizado correctamente.", "OK");
            await Shell.Current.GoToAsync("..");
        }
        else
        {
            ErrorLabel.Text = "Error al actualizar el perfil.";
            ErrorLabel.IsVisible = true;
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(".."); 
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
}