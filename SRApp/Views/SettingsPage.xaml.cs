using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage; // Para Preferences
using SRApp.Models;
using System.Net.Http;
using System.Net.Http.Json;
using System;

namespace SRApp.Views
{
    public partial class SettingsPage : ContentPage
    {
        public SettingsPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Recupera token y userId de Preferences
            var token = Preferences.Default.Get("AuthToken", string.Empty);
            var userId = Preferences.Default.Get("UserId", 0);

            // Si no hay credenciales, vuelve al login
            if (string.IsNullOrEmpty(token) || userId == 0)
            {
                await DisplayAlert("Error", "No se encontró sesión. Inicia sesión nuevamente.", "OK");
                return;
            }

            try
            {
                // Llama a GET /api/users/{id} para obtener datos completos
                using var client = new HttpClient();
                // Ajusta cabecera X-Token
                client.DefaultRequestHeaders.Remove("X-Token");
                client.DefaultRequestHeaders.Add("X-Token", token);

                string url = $"http://35.222.132.235:5164/api/users/{userId}";
                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var errorMsg = await response.Content.ReadAsStringAsync();
                    await DisplayAlert("Error", $"No se pudo obtener datos del usuario. {errorMsg}", "OK");
                    return;
                }

                // Parsea el usuario
                var user = await response.Content.ReadFromJsonAsync<User>();
                if (user == null)
                {
                    await DisplayAlert("Error", "Respuesta de usuario vacía.", "OK");
                    return;
                }

                // Muestra los labels
                UsernameLabel.Text = $"Nombre de usuario: {user.Username}";
                EmailLabel.Text = $"Email: {user.Email}";
                // Fecha de registro: formatea como quieras
                RegisterDateLabel.Text = $"Fecha de registro: {user.RegisteredAt:yyyy-MM-dd HH:mm}";

            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }

        private void OnLogoutClicked(object sender, EventArgs e)
        {
            // Borra token y userId de Preferences
            Preferences.Default.Remove("AuthToken");
            Preferences.Default.Remove("UserId");

            // Cambia la raíz a la página de Login
            App.SetRootPage(new NavigationPage(new LoginPage()));

        }
    }
}
