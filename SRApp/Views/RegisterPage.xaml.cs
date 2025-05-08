using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using SRApp.Models;
using SRApp.Services;

namespace SRApp.Views
{
    public partial class RegisterPage : ContentPage
    {
        public RegisterPage()
        {
            InitializeComponent();
        }

        private async void OnRegisterClicked(object sender, EventArgs e)
        {
            string username = UsernameEntry.Text;
            string email = EmailEntry.Text;
            string password = PasswordEntry.Text;

            // Validar que no estén vacíos
            if (string.IsNullOrEmpty(username) ||
                string.IsNullOrEmpty(email) ||
                string.IsNullOrEmpty(password))
            {
                await DisplayAlert("Error", "Todos los campos son obligatorios.", "OK");
                return;
            }

            // Validar formato de correo electrónico
            if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                await DisplayAlert("Error", "Por favor, introduce un correo electrónico válido.", "OK");
                return;
            }

            try
            {
                using var client = new HttpClient();

                // URL para el registro
                var registerUrl = "http://35.222.132.235:5164/api/users/register";
                var registerRequest = new
                {
                    Username = username,
                    Email = email,
                    Password = password
                };

                var registerResponse = await client.PostAsJsonAsync(registerUrl, registerRequest);
                if (registerResponse.IsSuccessStatusCode)
                {
                    // Limpia sesión previa
                    SessionService.Logout();

                    var loginUrl = "http://35.222.132.235/api/users/login";
                    var loginRequest = new LoginRequest
                    {
                        Email = email,
                        Password = password
                    };

                    var loginResponse = await client.PostAsJsonAsync(loginUrl, loginRequest);
                    if (loginResponse.IsSuccessStatusCode)
                    {
                        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
                        string token = loginResult?.Token ?? "";
                        int userId = loginResult?.UserId ?? 0;

                        if (string.IsNullOrEmpty(token) || userId == 0)
                        {
                            await DisplayAlert("Error", "Fallo en el login tras el registro.", "OK");
                            return;
                        }

                        // Guarda token y userId en Preferences
                        Preferences.Default.Set("AuthToken", token);
                        Preferences.Default.Set("UserId", userId);

                        // Cambia la página raíz a la principal de la app
                        App.SetRootPage(new AppShell());
                    }
                    else
                    {
                        var errorLogin = await loginResponse.Content.ReadAsStringAsync();
                        await DisplayAlert("Error", $"Registro completado, pero fallo en el login: {errorLogin}", "OK");
                    }
                }
                else
                {
                    var errorMessage = await registerResponse.Content.ReadAsStringAsync();
                    await DisplayAlert("Error", $"Error al registrar usuario: {errorMessage}", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }
    }
}
