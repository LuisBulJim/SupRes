using SRApp.Views;

namespace SRApp
{
    public partial class App : Application
    {
        private Page _rootPage;
        public static string? SelectedImagePath { get; set; }

        public App()
        {
            InitializeComponent();

            // Decide qué página mostrar según si hay token
            string token = Preferences.Default.Get("AuthToken", string.Empty);

            if (string.IsNullOrEmpty(token))
            {
                // Si no hay sesión muestra la LoginPage dentro de un NavigationPage
                _rootPage = new NavigationPage(new LoginPage());
            }
            else
            {
                // Si hay sesión muestra AppShell
                _rootPage = new AppShell();
            }
        }

        // Crear la ventana usando la página raíz elegida en el constructor
        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(_rootPage);
        }


        // Método para cambiar la página raíz en tiempo de ejecución.
        public static void SetRootPage(Page newRootPage)
        {
            if (Application.Current?.Windows != null && Application.Current.Windows.Count > 0)
            {
                var window = Application.Current.Windows[0];
                window.Page = newRootPage;
            }
        }

    }
}
