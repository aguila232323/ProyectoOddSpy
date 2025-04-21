namespace App
{
    public partial class AppShell : Shell
    {
        public bool IsAuthenticated { get; set; }

        public AppShell()
        {
            InitializeComponent();
            BindingContext = this;
        }

        public void SetAuthState(bool authenticated)
        {
            IsAuthenticated = authenticated;
            OnPropertyChanged(nameof(IsAuthenticated)); 
        }
    }
}