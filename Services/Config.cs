using System;
using System.Reflection;

namespace SqlToRestApi.Services
{
    internal class Config
    {
        public static string AppPath
        {
            get
            {
                var location = Assembly.GetExecutingAssembly().Location;
                return location != null ? System.IO.Path.GetDirectoryName(location.Trim()) : "";
            }
        }

        public static string AppName => Assembly.GetExecutingAssembly().GetName().Name.Trim();

        public static string ApiDbConnectionString { set; get; }

        public static int SqlCommandTimeout { set; get; }

        public static bool AuthorizationRequired { get; set; }

        public static void Read()
        {
            ApiDbConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["ApiDb"].ConnectionString;
            try
            {
                SqlCommandTimeout = int.Parse(System.Configuration.ConfigurationManager.AppSettings["SqlCommandTimeout"]);
            }
            catch (Exception)
            {
                SqlCommandTimeout = 900;
            }

            try
            {
                AuthorizationRequired = bool.Parse(System.Configuration.ConfigurationManager.AppSettings["AuthorizationRequired"]);
            }
            catch (Exception)
            {
                AuthorizationRequired = false;
            }
        }

        static Config()
        {
            Read();
        }
    }
}