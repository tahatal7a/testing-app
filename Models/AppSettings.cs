using System;

namespace DesktopTaskAid.Models
{
    public class AppSettings
    {
        public string Theme { get; set; } // "light" or "dark"
        public bool HelperEnabled { get; set; }

        public AppSettings()
        {
            Theme = "light";
            HelperEnabled = false;
        }
    }
}
