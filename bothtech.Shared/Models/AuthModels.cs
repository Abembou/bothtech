using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


    namespace BothTech.Models
    {
        public class AuthRequest
        {
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public bool ReturnSecureToken { get; set; } = true;
        }

        public class AuthResponse
        {
            public string IdToken { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string RefreshToken { get; set; } = string.Empty;
            public string ExpiresIn { get; set; } = string.Empty;
            public string LocalId { get; set; } = string.Empty;
            public string Error { get; set; } = string.Empty; // En cas d'erreur
        }
    }

