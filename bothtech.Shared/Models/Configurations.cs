using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bothtech.Shared.Models
{
   
        public class AppSettings
        {
            public Auth0Settings Auth0 { get; set; } = new();
            public FirebaseSettings Firebase { get; set; } = new();
        }

        public class Auth0Settings
        {
            public string Domain { get; set; } = "";
            public string ClientId { get; set; } = "";
            public string ClientSecret { get; set; } = "";
            public string RedirectUri { get; set; } = "";
        }

        public class FirebaseSettings
        {
            public string DatabaseUrl { get; set; } = "";
            public string ApiKey { get; set; } = "";
            public string ProjectId { get; set; } = "";
        }
    
}
