using System;
using Microsoft.Maui.Devices;

namespace SRApp
{
    public static class ApiConfig
    {
        public static string BaseAddress =>
            DeviceInfo.Platform == DevicePlatform.Android
                ? "http://35.222.132.235:5164"  // Para Android Emulator (reemplaza localhost)
                : "http://localhost:5164"; // Para iOS Simulator, Windows, etc.
    }
}
