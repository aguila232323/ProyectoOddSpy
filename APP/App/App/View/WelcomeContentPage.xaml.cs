using SkiaSharp.Extended.UI.Controls;

namespace App.View;

public partial class WelcomeContentPage : ContentPage
{
    public int AnimationRepeatCount => -1;
    public SKLottieRepeatMode AnimationRepeatMode => SKLottieRepeatMode.Reverse;
    public string AnimationSource => "mafia.json";
    public WelcomeContentPage()
	{
		InitializeComponent();
	}
    private async void OnNavigateButtonClickedLogin(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new Login());
    }
    private async void OnRegisterTapped(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new Register());
    }
}