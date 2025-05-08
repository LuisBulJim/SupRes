using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using SRApp.Models;

namespace SRApp.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;

        public ApiService()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(ApiConfig.BaseAddress),
                Timeout = TimeSpan.FromSeconds(10)
            };
        }

        // Establece el token en la cabecera "X-Token".
        private void SetToken(string token)
        {
            if (_httpClient.DefaultRequestHeaders.Contains("X-Token"))
            {
                _httpClient.DefaultRequestHeaders.Remove("X-Token");
            }
            _httpClient.DefaultRequestHeaders.Add("X-Token", token);
        }


        // GET /api/images -> Devuelve todas las imágenes.
        public async Task<List<ProcessedImage>> GetImagesAsync(string token)
        {
            try
            {
                SetToken(token);
                var result = await _httpClient.GetFromJsonAsync<List<ProcessedImage>>("api/images");
                return result ?? new List<ProcessedImage>();
            }
            catch (Exception ex)
            {
                throw new HttpRequestException($"Error al obtener imágenes: {ex.Message}", ex);
            }
        }


        // GET /api/images/user/{userId} -> Devuelve las imágenes de un usuario específico.
        public async Task<List<ProcessedImage>> GetImagesByUserAsync(int userId, string token)
        {
            try
            {
                // Asegura que el token se pase correctamente a la cabecera.
                _httpClient.DefaultRequestHeaders.Remove("X-Token");
                _httpClient.DefaultRequestHeaders.Add("X-Token", token);

                var url = $"api/images/user/{userId}";
                System.Diagnostics.Debug.WriteLine($"Request URL: {_httpClient.BaseAddress}{url}");

                var result = await _httpClient.GetFromJsonAsync<List<ProcessedImage>>(url);
                return result ?? new List<ProcessedImage>();
            }
            catch (Exception ex)
            {
                throw new HttpRequestException($"Error al obtener imágenes del usuario {userId}: {ex.Message}", ex);
            }
        }


        // POST /api/images -> Crea una nueva imagen en el servidor.
        public async Task<ProcessedImage> CreateImageAsync(ProcessedImage image, string token)
        {
            try
            {
                SetToken(token);
                var response = await _httpClient.PostAsJsonAsync("api/images", image);
                response.EnsureSuccessStatusCode();

                var created = await response.Content.ReadFromJsonAsync<ProcessedImage>();
                if (created != null)
                {
                    // Si ProcessedAt viene con el valor por defecto, se actualiza a DateTime.UtcNow.
                    if (created.ProcessedAt == default(DateTime))
                    {
                        created.ProcessedAt = DateTime.UtcNow;
                    }
                    return created;
                }
                throw new Exception("La creación de la imagen retornó null.");
            }
            catch (Exception ex)
            {
                throw new HttpRequestException($"Error al crear la imagen: {ex.Message}", ex);
            }
        }


        // GET /api/images/my -> Obtiene las imágenes del usuario autenticado.
        public async Task<List<ProcessedImage>> GetMyImagesAsync(string token)
        {
            try
            {
                SetToken(token);
                var result = await _httpClient.GetFromJsonAsync<List<ProcessedImage>>("api/images/my");
                return result ?? new List<ProcessedImage>();
            }
            catch (Exception ex)
            {
                throw new HttpRequestException($"Error al obtener tus imágenes: {ex.Message}", ex);
            }
        }


        // PUT /api/images/{id} -> Actualiza una imagen existente en el servidor.
        public async Task UpdateImageAsync(int id, ProcessedImage image, string token)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Remove("X-Token");
                _httpClient.DefaultRequestHeaders.Add("X-Token", token);
                var response = await _httpClient.PutAsJsonAsync($"api/images/{id}", image);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                throw new HttpRequestException($"Error al actualizar la imagen: {ex.Message}", ex);
            }
        }

        public async Task<List<ProcessedImage>> GetImagesByUserAsync(int userId, string token, CancellationToken ct)
        {
            // Quita y añade la cabecera "X-Token"
            _httpClient.DefaultRequestHeaders.Remove("X-Token");
            _httpClient.DefaultRequestHeaders.Add("X-Token", token);

            var url = $"api/images/user/{userId}";
            var result = await _httpClient.GetFromJsonAsync<List<ProcessedImage>>(
                url,
                ct
            );
            return result ?? new List<ProcessedImage>();
        }

    }
}
