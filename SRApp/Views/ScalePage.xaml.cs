using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace SRApp.Views
{
    public partial class ScalePage : ContentPage
    {
        public double SelectedScaleFactor { get; set; }
        private Border? _selectedScaleBorder;

        public ScalePage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Vista previa si hay imagen seleccionada
            if (!string.IsNullOrEmpty(App.SelectedImagePath))
            {
                PreviewImage.Source = ImageSource.FromFile(App.SelectedImagePath);
            }
            else
            {
                DisplayAlert("Info", "No se ha seleccionado ninguna imagen.", "OK");
            }
        }

        // Animacion de la selección
        private async Task AnimateSelection(Border tappedBorder, double scaleFactor)
        {
            Border[] scaleBorders = { frameScale2, frameScale3, frameScale4, frameScale4Real };
            var animationTasks = new List<Task>();

            foreach (var border in scaleBorders)
            {
                double targetOpacity = (border == tappedBorder) ? 1.0 : 0.5;
                animationTasks.Add(border.FadeTo(targetOpacity, 100));
            }

            await Task.WhenAll(animationTasks);

            _selectedScaleBorder = tappedBorder;
            SelectedScaleFactor = scaleFactor;
        }

        private async void OnScale2Tapped(object sender, EventArgs e)
        {
            if (sender is Border border)
                await AnimateSelection(border, 2);
        }

        private async void OnScale3Tapped(object sender, EventArgs e)
        {
            if (sender is Border border)
                await AnimateSelection(border, 3);
        }

        private async void OnScale4Tapped(object sender, EventArgs e)
        {
            if (sender is Border border)
                await AnimateSelection(border, 4);
        }

        private async void OnScale4RealTapped(object sender, EventArgs e)
        {
            if (sender is Border border)
                await AnimateSelection(border, 4.5);
        }

        // Pulsar "Comenzar reescalado" -> navega a LoadingPage
        private async void OnStartScalingClicked(object sender, EventArgs e)
        {
            // Validar imagen
            if (string.IsNullOrEmpty(App.SelectedImagePath))
            {
                await DisplayAlert("Error", "Debes seleccionar una imagen primero.", "OK");
                return;
            }

            // Validar factor
            if (SelectedScaleFactor <= 0)
            {
                await DisplayAlert("Error", "Debes seleccionar un factor de reescalado.", "OK");
                return;
            }

            // Navegar a LoadingPage
            await Navigation.PushAsync(new LoadingPage(App.SelectedImagePath, SelectedScaleFactor));
        }
    }
}
