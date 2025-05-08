using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace SRApp.Views;

public partial class AddPage : ContentPage
{
    public AddPage()
    {
        InitializeComponent();
    }

    private async void OnScanTapped(object sender, EventArgs e)
    {
        try
        {
            var photo = await MediaPicker.CapturePhotoAsync();
            if (photo != null)
            {
                string localFilePath = await AddPage.SaveFileToLocal(photo);
                App.SelectedImagePath = localFilePath; // Guardamos la imagen en App
                await AddPage.NavigateToScalePage();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo abrir la cámara: {ex.Message}", "OK");
        }
    }

    private async void OnPickFromLibraryTapped(object sender, EventArgs e)
    {
        try
        {
            var photo = await MediaPicker.PickPhotoAsync();
            if (photo != null)
            {
                string localFilePath = await AddPage.SaveFileToLocal(photo);
                App.SelectedImagePath = localFilePath; // Guardamos la imagen en App
                await AddPage.NavigateToScalePage();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo seleccionar la imagen: {ex.Message}", "OK");
        }
    }

    private static async Task<string> SaveFileToLocal(FileResult photo)
    {
        if (photo == null)
            return string.Empty;

        string localFilePath = Path.Combine(FileSystem.CacheDirectory, photo.FileName);

        using (var stream = await photo.OpenReadAsync())
        using (var newStream = File.OpenWrite(localFilePath))
        {
            await stream.CopyToAsync(newStream);
        }

        return localFilePath;
    }


    private static async Task NavigateToScalePage()
    {
        await Shell.Current.GoToAsync("ScalePage");
    }
}
