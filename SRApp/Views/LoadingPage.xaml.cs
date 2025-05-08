using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage; // Para Preferences
using SRApp.Models;
using SRApp.Services;
using System.Net.Http.Json;

// Para FileSaver (API antigua), si usas CommunityToolkit.Maui.Storage
using CommunityToolkit.Maui.Storage;

namespace SRApp.Views
{
    public partial class LoadingPage : ContentPage
    {
        // Ruta de la imagen original seleccionada.
        public string SelectedImagePath { get; set; }
        public double SelectedScaleFactor { get; set; }
        
        // Registro de imagen pendiente creado en la DB.
        private ProcessedImage? _pendingImageRecord;

        // Evitar reprocesar la imagen al volver a la pantalla.
        private bool _processingDone = false;

        // Servicio de API con métodos de instancia.
        private readonly ApiService _apiService = new();

        // Guardaremos la ruta temporal donde se escribió la imagen reescalada
        private string? _processedTempPath;

        public LoadingPage(string imagePath, double scaleFactor)
        {
            InitializeComponent();
            SelectedImagePath = imagePath;
            SelectedScaleFactor = scaleFactor;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (!_processingDone)
            {
                _processingDone = true;
                await ProcessImageAsync();
            }
        }

        private async Task ProcessImageAsync()
        {
            // Crea pendiente
            _pendingImageRecord = await CreatePendingImageAsync();
            if (_pendingImageRecord == null)
            {
                // No se pudo crear en la DB => salimos
                await DisplayAlert("Error", "No se pudo crear el registro pendiente.", "OK");
                await Navigation.PopAsync();
                return;
            }

            // Determinar contenedor Docker y endpoint
            string baseUrl, endpoint;
            switch (SelectedScaleFactor)
            {
                case 2:
                    baseUrl = "http://34.60.206.109:7860";
                    endpoint = "enhanceX2";
                    break;
                case 3:
                    baseUrl = "http://34.60.206.109:7860";
                    endpoint = "enhanceX3";
                    break;
                case 4:
                    baseUrl = "http://34.60.206.109:7860";
                    endpoint = "enhanceX4";
                    break;
                case 4.5:
                    baseUrl = "http://34.60.206.109:7860";
                    endpoint = "enhanceX4Real";
                    break;
                default:
                    await DisplayAlert("Error", "Factor de reescalado no reconocido.", "OK");
                    return;
            }

            // Verificar Docker
            bool dockerAlive = await LoadingPage.CheckDockerAvailabilityAsync(baseUrl);
            if (!dockerAlive)
            {
                // Docker no disponible => marcar error en la DB
                await MarkImageAsErrorAsync(_pendingImageRecord.ImageId);
                await DisplayAlert("Error", "El servidor de reescalado no está disponible.", "OK");
                await Navigation.PopAsync();
                return;
            }

            // Llamar a Docker con la imagen original
            string url = $"{baseUrl}/{endpoint}";
            try
            {
                using var client = new HttpClient();
                using var multipartContent = new MultipartFormDataContent();

                byte[] imageBytes = File.ReadAllBytes(SelectedImagePath);
                var imageContent = new ByteArrayContent(imageBytes);
                imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
                multipartContent.Add(imageContent, "file", Path.GetFileName(SelectedImagePath));

                var response = await client.PostAsync(url, multipartContent);
                if (response.IsSuccessStatusCode)
                {
                    // Recibir la imagen procesada
                    byte[] responseBytes = await response.Content.ReadAsByteArrayAsync();
                    _processedTempPath = Path.Combine(FileSystem.CacheDirectory, $"enhanced_{Guid.NewGuid()}.png");
                    File.WriteAllBytes(_processedTempPath, responseBytes);

                    // Mostrar la procesada
                    resultImage.Source = ImageSource.FromFile(_processedTempPath);
                    resultImage.IsVisible = true;
                    loadingLabel.IsVisible = false;
                    activityIndicator.IsRunning = false;

                    // Mostrar botón de descarga
                    DownloadReescaladaButton.IsVisible = true;

                    // Subir la procesada a la API => /api/images/upload
                    var updatedRecord = await UploadProcessedImageAsync(_processedTempPath);
                    if (updatedRecord != null)
                    {
                        _pendingImageRecord = updatedRecord;
                    }
                }
                else
                {
                    // Falla Docker => marcar error en la DB
                    _pendingImageRecord.Status = "error";
                    _pendingImageRecord.ProcessedAt = DateTime.UtcNow;
                    await _apiService.UpdateImageAsync(_pendingImageRecord.ImageId, _pendingImageRecord,
                        Preferences.Default.Get("AuthToken", string.Empty));

                    await DisplayAlert("Error", "Error al procesar la imagen en Docker.", "OK");
                }
            }
            catch (Exception ex)
            {
                // Excepción => marcar error
                _pendingImageRecord.Status = "error";
                _pendingImageRecord.ProcessedAt = DateTime.UtcNow;
                await _apiService.UpdateImageAsync(_pendingImageRecord.ImageId, _pendingImageRecord,
                    Preferences.Default.Get("AuthToken", string.Empty));

                await DisplayAlert("Error", $"Ocurrió un error: {ex.Message}", "OK");
            }
        }

        /// <summary>
        /// Crea la imagen pendiente enviando la original a /api/images/pending (multipart/form-data).
        /// </summary>
        private async Task<ProcessedImage?> CreatePendingImageAsync()
        {
            var token = Preferences.Default.Get("AuthToken", string.Empty);
            var userId = Preferences.Default.Get("UserId", 0);
            if (string.IsNullOrEmpty(token) || userId == 0)
            {
                await DisplayAlert("Error", "No se encontró información de usuario. Inicia sesión.", "OK");
                return null;
            }

            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Remove("X-Token");
                client.DefaultRequestHeaders.Add("X-Token", token);

                using var form = new MultipartFormDataContent();

                // Campos de texto
                form.Add(new StringContent(userId.ToString()), "UserId");
                string scale = SelectedScaleFactor switch
                {
                    2 => "x2",
                    3 => "x3",
                    4 => "x4",
                    4.5 => "x4Real",
                    _ => "desconocido"
                };
                form.Add(new StringContent(scale), "ScaleOption");
                form.Add(new StringContent("Pendiente de procesamiento"), "Metadata");

                // Imagen original
                byte[] originalBytes = File.ReadAllBytes(SelectedImagePath);
                var originalFileContent = new ByteArrayContent(originalBytes);
                originalFileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
                form.Add(originalFileContent, "OriginalFile", Path.GetFileName(SelectedImagePath));

                // POST a /api/images/pending
                string url = "http://35.222.132.235:5164/api/images/pending";
                var response = await client.PostAsync(url, form);

                if (response.IsSuccessStatusCode)
                {
                    var created = await response.Content.ReadFromJsonAsync<ProcessedImage>();
                    return created;
                }
                else
                {
                    string errorBody = await response.Content.ReadAsStringAsync();
                    await DisplayAlert("Error", $"No se pudo crear pendiente: {errorBody}", "OK");
                    return null;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
                return null;
            }
        }

        /// <summary>
        /// Sube la imagen procesada a /api/images/upload para actualizar la fila en "procesada".
        /// </summary>
        private async Task<ProcessedImage?> UploadProcessedImageAsync(string processedImagePath)
        {
            var token = Preferences.Default.Get("AuthToken", string.Empty);
            var userId = Preferences.Default.Get("UserId", 0);
            if (string.IsNullOrEmpty(token) || userId == 0)
            {
                await DisplayAlert("Error", "No se encontró información de usuario.", "OK");
                return null;
            }

            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Remove("X-Token");
                client.DefaultRequestHeaders.Add("X-Token", token);

                using var formContent = new MultipartFormDataContent();

                // Indicar el ImageId de la imagen pendiente para actualizar
                formContent.Add(new StringContent(_pendingImageRecord!.ImageId.ToString()), "ImageId");
                formContent.Add(new StringContent(userId.ToString()), "UserId");

                string scaleOption = SelectedScaleFactor switch
                {
                    2 => "x2",
                    3 => "x3",
                    4 => "x4",
                    4.5 => "x4Real",
                    _ => "desconocido"
                };
                formContent.Add(new StringContent(scaleOption), "ScaleOption");
                formContent.Add(new StringContent("Parámetros de escalado"), "Metadata");

                // Ruta de la imagen original (por si quieres sobrescribir)
                formContent.Add(new StringContent(Path.GetFileName(SelectedImagePath)), "OriginalImagePath");

                // Sube la procesada
                byte[] processedFileBytes = File.ReadAllBytes(processedImagePath);
                var processedFileContent = new ByteArrayContent(processedFileBytes);
                processedFileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
                formContent.Add(processedFileContent, "ProcessedFile", Path.GetFileName(processedImagePath));

                string uploadUrl = "http://35.222.132.235:5164/api/images/upload";
                var response = await client.PostAsync(uploadUrl, formContent);

                if (response.IsSuccessStatusCode)
                {
                    var updated = await response.Content.ReadFromJsonAsync<ProcessedImage>();
                    return updated;
                }
                else
                {
                    string errorMsg = await response.Content.ReadAsStringAsync();
                    await DisplayAlert("Error", $"No se pudo actualizar la imagen: {errorMsg}", "OK");
                    return null;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
                return null;
            }
        }

        /// <summary>
        /// Llama a /api/images/set-error para marcar la imagen con ID dado como "error".
        /// </summary>
        private async Task MarkImageAsErrorAsync(int imageId)
        {
            var token = Preferences.Default.Get("AuthToken", string.Empty);
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Remove("X-Token");
            client.DefaultRequestHeaders.Add("X-Token", token);

            var response = await client.PostAsJsonAsync("http://35.222.132.235:5164/api/images/set-error", imageId);
            if (response.IsSuccessStatusCode)
            {
                var updated = await response.Content.ReadFromJsonAsync<ProcessedImage>();
                // Maneja la info si lo deseas
            }
            else
            {
                string err = await response.Content.ReadAsStringAsync();
                await DisplayAlert("Error", $"No se pudo marcar la imagen como error: {err}", "OK");
            }
        }

        /// <summary>
        /// Verifica que Docker esté disponible llamando a /ping
        /// </summary>
        private static async Task<bool> CheckDockerAvailabilityAsync(string baseUrl)
        {
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
                string pingUrl = $"{baseUrl}/ping";
                var response = await client.GetAsync(pingUrl);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Botón para descargar la imagen reescalada con la API antigua de FileSaver
        /// </summary>
        private async void OnDownloadReescaladaClicked(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(_processedTempPath) || !File.Exists(_processedTempPath))
                {
                    await DisplayAlert("Error", "No se encontró la imagen reescalada en caché.", "OK");
                    return;
                }

                byte[] reescaladaBytes = File.ReadAllBytes(_processedTempPath);

                using var ms = new MemoryStream(reescaladaBytes);

                var suggestedFileName = "ImagenReescalada.png";
                var result = await FileSaver.Default.SaveAsync(suggestedFileName, ms);

                if (result.IsSuccessful)
                {
                    // Aquí navegas a AddPage, ya sea con Shell:
                     await Navigation.PushAsync(new AddPage());
                }
                else
                {
                    await DisplayAlert("Cancelado", "No se guardó la imagen reescalada.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }

    }
}
