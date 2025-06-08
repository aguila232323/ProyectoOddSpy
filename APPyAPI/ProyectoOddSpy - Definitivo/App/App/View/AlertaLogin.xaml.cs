using CommunityToolkit.Maui.Views;

namespace App.View;

public partial class AlertaLogin : Popup
{
	
    public AlertaLogin(string message)
    {
        InitializeComponent();
        
    }

    private void OnCloseClicked(object sender, EventArgs e)
    {
        Close();
    }
}