using SRApp.Models;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.Maui.Storage; // Para Preferences
using SRApp.Services; // Para SessionService

namespace SRApp.Views
{
    public partial class LoginPage : ContentPage
    {
        public LoginPage()
        {
            InitializeComponent();
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            string email = EmailEntry.Text;
            string password = PasswordEntry.Text;

            // Validación
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                await DisplayAlert("Error", "Por favor, ingresa tu correo y contraseña.", "OK");
                return;
            }

            try
            {
                using (var client = new HttpClient())
                {
                    var url = "http://35.222.132.235:5164/api/users/login";
                    var request = new LoginRequest
                    {
                        Email = email,
                        Password = password
                    };

                    var response = await client.PostAsJsonAsync(url, request);
                    if (response.IsSuccessStatusCode)
                    {
                        // Limpiar sesión previa si hubiera
                        SessionService.Logout();

                        var loginResp = await response.Content.ReadFromJsonAsync<LoginResponse>();
                        string token = loginResp?.Token ?? "";
                        int userId = loginResp?.UserId ?? 0;

                        if (string.IsNullOrEmpty(token) || userId == 0)
                        {
                            await DisplayAlert("Error", "Error en la respuesta del login. Inténtalo de nuevo.", "OK");
                            return;
                        }

                        // Guarda token y userId en Preferences
                        Preferences.Default.Set("AuthToken", token);
                        Preferences.Default.Set("UserId", userId);

                        // Cambiar la página raíz a la AppShell
                        App.SetRootPage(new AppShell());
                    }
                    else
                    {
                        var errorMessage = await response.Content.ReadAsStringAsync();
                        await DisplayAlert("Error", $"Datos de acceso incorrectos. {errorMessage}", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }

        private async void OnRegisterClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new RegisterPage());
        }
    }
}
