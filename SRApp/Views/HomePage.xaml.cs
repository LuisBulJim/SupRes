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
            // Navega a la p�gina de configuraci�n
            await Navigation.PushAsync(new SettingsPage());
        }
    }
}
