using SRApp.Views;

namespace SRApp;

public partial class AppShell : Shell
{
    public Command<string> NavigateCommand { get; }
    public AppShell()
    {
        InitializeComponent();
        NavigateCommand = new Command<string>(NavigateToPage);
        BindingContext = this;
        Routing.RegisterRoute(nameof(ScalePage), typeof(ScalePage));
    }
    private async void NavigateToPage(string page)
    {
        await Shell.Current.GoToAsync($"//{page}");
    }
}
