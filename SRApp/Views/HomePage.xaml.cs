using System;
using Microsoft.Maui.Controls;

namespace SRApp.Views
{
    public partial class HomePage : ContentPage
    {
        public HomePage()
        {
            InitializeComponent();
        }

        private async void OnSettingsClicked(object sender, EventArgs e)
        {
            // Navega a la página de configuración
            await Navigation.PushAsync(new SettingsPage());
        }
    }
}
