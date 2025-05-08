using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.PlatformConfiguration;
using SRApp.Models;
using CommunityToolkit.Maui.Storage;
using System.Net.Http;

namespace SRApp.Views
{
    public partial class ImageDetailPage : ContentPage
    {
        private ProcessedImage _selectedImage;

        public ImageDetailPage(ProcessedImage selectedImage)
        {
            InitializeComponent();
            _selectedImage = selectedImage;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Muestra el estado
            StatusLabel.Text = $"Estado: {_selectedImage.Status}";

            // Muestra la escala elegida
            ScaleDetailLabel.Text = $"Escala elegida: {_selectedImage.ScaleOption}";
            ScaleDetailLabel.IsVisible = true;

            // Decidimos la lógica según el estado
            switch (_selectedImage.Status)
            {
                case "pendiente":
                case "error":
                    // Muestra la imagen original "Outside"
                    OriginalImageOutside.IsVisible = true;
                    OriginalImageOutside.Source = _selectedImage.FullOriginalImageUrl;

                    // Permite descargar la original
                    DownloadOriginalButton.IsVisible = true;

                    // No muestra el panel => ComparisonPanel.IsVisible = false
                    ComparisonPanel.IsVisible = false;
                    break;

                case "procesada":
                    // Muestra panel de comparación
                    ComparisonPanel.IsVisible = true;

                    // Asigna la original dentro del panel
                    OriginalImageInPanel.Source = _selectedImage.FullOriginalImageUrl;

                    // Asigna la procesada para el recorte
                    ProcessedImage.Source = _selectedImage.FullProcessedImageUrl;

                    // Descargar original y procesada
                    DownloadOriginalButton.IsVisible = true;
                    DownloadProcessedButton.IsVisible = true;

                    // Suscribimos el evento SizeChanged para la procesada
                    ProcessedImage.SizeChanged += OnImageSizeChanged;
                    break;

                default:
                    // Estado desconocido => no hacemos nada especial
                    break;
            }
        }

        // Cuando la imagen procesada ajusta su tamaño
        private async void OnImageSizeChanged(object? sender, EventArgs e)
        {
            ProcessedImage.SizeChanged -= OnImageSizeChanged;

            // Ajustar recorte inicial
            UpdateClip(ComparisonSlider.Value);

            await Task.Delay(200);

            // Anima de 0 a 1
            await AnimateSliderValueAsync(0, 1, 1000);

            // Luego 1 a 0.5
            await AnimateSliderValueAsync(1, 0.5, 1000);
        }

        // Slider => recorta la imagen procesada
        private void OnSliderValueChanged(object sender, ValueChangedEventArgs e)
        {
            UpdateClip(e.NewValue);
        }

        private void UpdateClip(double fraction)
        {
            double totalWidth = ProcessedImage.Width;
            double totalHeight = ProcessedImage.Height;
            if (totalWidth <= 0 || totalHeight <= 0)
                return;

            double newWidth = fraction * totalWidth;
            ClipRectangle.Rect = new Rect(0, 0, newWidth, totalHeight);

            // Línea divisoria
            DividerLine.IsVisible = true;
            AbsoluteLayout.SetLayoutBounds(
                DividerLine,
                new Rect(
                    newWidth - DividerLine.WidthRequest / 2,
                    0,
                    DividerLine.WidthRequest,
                    totalHeight
                )
            );

            double sliderWidth = 200;
            double sliderHeight = 40;
            double gap = 10;
            AbsoluteLayout.SetLayoutBounds(
                ComparisonSlider,
                new Rect(
                    newWidth - (sliderWidth / 2),
                    totalHeight + gap,
                    sliderWidth,
                    sliderHeight
                )
            );
        }

        private Task AnimateSliderValueAsync(double from, double to, uint duration)
        {
            var tcs = new TaskCompletionSource<bool>();
            var animation = new Animation(v => ComparisonSlider.Value = v, from, to);
            animation.Commit(this, "SliderAnim", 16, duration, Easing.Linear,
                (v, c) => tcs.SetResult(true));
            return tcs.Task;
        }

        // Descargar la imagen original


        private async void OnDownloadOriginalClicked(object sender, EventArgs e)
        {
            try
            {
                var url = _selectedImage.FullOriginalImageUrl;

                // 1) Descargar la imagen en memoria
                using var httpClient = new HttpClient();
                byte[] imageBytes = await httpClient.GetByteArrayAsync(url);

                // 2) Convierte los bytes en un Stream
                using var ms = new MemoryStream(imageBytes);

                // 3) Llama a la antigua sobrecarga de SaveAsync
                var fileName = "MiImagen.png";
                var result = await FileSaver.Default.SaveAsync(fileName, ms);
                // (Opcional) var result = await FileSaver.Default.SaveAsync(
                //     fileName, ms, cancellationToken: cts.Token);

                if (result.IsSuccessful)
                {
                    await DisplayAlert("OK", "Imagen guardada correctamente.", "OK");
                }
                else
                {
                    // El usuario canceló la acción de guardar
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }






        // Descargar la imagen reescalada
        private async void OnDownloadProcessedClicked(object sender, EventArgs e)
        {
            try
            {
                // 1) Obtiene la URL de la imagen reescalada
                var url = _selectedImage.FullProcessedImageUrl;

                // 2) Descarga la imagen en memoria
                using var httpClient = new HttpClient();
                byte[] imageBytes = await httpClient.GetByteArrayAsync(url);

                // 3) Convierte los bytes en un Stream
                using var ms = new MemoryStream(imageBytes);

                // 4) Invoca al FileSaver sin SaveFileOptions,
                //    abriendo el diálogo "Guardar como" en cada plataforma
                var suggestedFileName = "ImagenReescalada.png";
                var result = await FileSaver.Default.SaveAsync(suggestedFileName, ms);

                if (result.IsSuccessful)
                {
                    // Se guardó correctamente
                    //await DisplayAlert("OK", "La imagen reescalada se guardó con éxito.", "OK");
                }
                else
                {
                    // El usuario canceló la acción de guardar
                    await DisplayAlert("Cancelado", "No se guardó la imagen reescalada.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }


        private async void OnDeleteImageClicked(object sender, EventArgs e)
        {
            // Confirmar la acción con el usuario
            bool confirm = await DisplayAlert("Confirmar",
                "¿Estás seguro de eliminar la imagen?",
                "Sí", "No");
            if (!confirm)
                return;

            try
            {
                // Llama a tu API para eliminar la imagen por su ID
                // Asumiendo que tu server en DELETE /api/images/{id} 
                // borra la fila y el archivo del servidor.

                var token = Preferences.Default.Get("AuthToken", string.Empty);
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Remove("X-Token");
                client.DefaultRequestHeaders.Add("X-Token", token);

                var url = $"http://35.222.132.235:5164/api/images/{_selectedImage.ImageId}";
                var response = await client.DeleteAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    // Se eliminó con éxito en el servidor

                    // Opcional: si mantenías algún archivo local, bórralo también
                    // File.Delete(localPath) si lo tuvieras.

                    // Regresa a la página anterior o donde gustes
                    await Navigation.PopAsync();
                }
                else
                {
                    // Muestra el error que devuelva la API
                    var errorMsg = await response.Content.ReadAsStringAsync();
                    await DisplayAlert("Error", $"No se pudo eliminar la imagen: {errorMsg}", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }

    }
}
