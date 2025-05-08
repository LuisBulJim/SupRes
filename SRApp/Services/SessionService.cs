using Microsoft.Maui.Storage;

namespace SRApp.Services
{
    public static class SessionService
    {
        // Limpia las preferencias relacionadas con la sesión.
        public static void Logout()
        {
            Preferences.Default.Remove("AuthToken");
            Preferences.Default.Remove("UserId");
        }
    }


}
