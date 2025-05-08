using System;
using System.Collections.Generic;

namespace SRApp.Models
{
    public class User
    {
        public int UserId { get; set; }            
        public required string Username { get; set; }
        public required string Email { get; set; }
        public required string PasswordHash { get; set; }
        public DateTime RegisteredAt { get; set; }
        public List<ProcessedImage> Images { get; set; } = new List<ProcessedImage>();
    }

    // Para iniciar sesion con el email y la contraseña
    public class LoginRequest
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
    }

    // Respuesta del login
    public class LoginResponse
    {
        public required string Token { get; set; }
        public int UserId { get; set; }
    }
}
