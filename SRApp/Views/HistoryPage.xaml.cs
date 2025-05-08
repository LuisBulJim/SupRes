using SRApp.Models;
using SRApp.Services;
using Microsoft.Maui.Storage; // Para Preferences
using System.Threading;

namespace SRApp.Views
{
    public partial class HistoryPage : ContentPage
    {
        private readonly ApiService _apiService = new();

        public HistoryPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Oculta labels y CollectionView antes de intentar conexión
            NoConnectionLabel.IsVisible = false;
            NoImagesLabel.IsVisible = false;
            UserImagesCollection.IsVisible = false;

            try
            {
                var token = Preferences.Default.Get("AuthToken", string.Empty);
                var userId = Preferences.Default.Get("UserId", 0);

                if (string.IsNullOrEmpty(token) || userId == 0)
                {
                    // Muestra "No hay imágenes" si no hay userId
                    NoImagesLabel.IsVisible = true;
                    return;
                }

                // Intenta obtener imágenes con timeout de 4s
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(4));

                // Llama al endpoint con un token de cancelación
                var images = await _apiService.GetImagesByUserAsync(userId, token, cts.Token);

                // Ordenar de más reciente a más antigua
                images = images.OrderByDescending(img => img.ImageId).ToList();

                if (images.Count == 0)
                {
                    NoImagesLabel.IsVisible = true;
                }
                else
                {
                    UserImagesCollection.IsVisible = true;
                    UserImagesCollection.ItemsSource = images;
                }
            }
            catch (OperationCanceledException)
            {
                // Tiempo de 2s excedido => No hay conexión
                NoConnectionLabel.IsVisible = true;
            }
            catch
            {
                // Cualquier otro error => asume sin conexión
                NoConnectionLabel.IsVisible = true;
            }
        }

        private async void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is ProcessedImage selectedImage)
            {
                // Navega a la página de detalle
                await Navigation.PushAsync(new ImageDetailPage(selectedImage));
                UserImagesCollection.SelectedItem = null;
            }
        }
    }
}
