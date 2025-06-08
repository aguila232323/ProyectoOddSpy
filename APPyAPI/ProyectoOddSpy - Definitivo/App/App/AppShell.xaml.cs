using App.View;

namespace App
{
    public partial class AppShell : Shell
    {
        public bool IsAuthenticated { get; set; }

        public AppShell()
        {
            InitializeComponent();
            BindingContext = this;
            Routing.RegisterRoute(nameof(SureBetDetail), typeof(SureBetDetail));
            Routing.RegisterRoute(nameof(ComparerDetail), typeof(ComparerDetail));
            Routing.RegisterRoute(nameof(OffersDetails), typeof(OffersDetails));
            Routing.RegisterRoute("EditProfilePage", typeof(EditProfilePage));
        }

        public void SetAuthState(bool authenticated)
        {
            IsAuthenticated = authenticated;
            OnPropertyChanged(nameof(IsAuthenticated)); 
        }
        private async void ProfileIcon_Clicked(object sender, EventArgs e)
        {
            await this.FadeTo(0.9, 100);
            await Navigation.PushModalAsync(new ProfilePage(), true);
            await this.FadeTo(1, 100);
        }


    }
}