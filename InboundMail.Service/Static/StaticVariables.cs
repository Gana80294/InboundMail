using EmamiInboundMail.Service.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmamiInboundMail.Service.Static
{
    public static class StaticVariables
    {

        static StaticVariables()
        {
            try
            {
                NpgSqlConnectionString = ConfigurationManager.ConnectionStrings["NpgSqlAuthContext"].ConnectionString;
                SqlConnectionString = ConfigurationManager.ConnectionStrings["SqlAuthContext"].ConnectionString;

                ClientId = ConfigurationManager.AppSettings["ClientId"];
                ClientSecret = ConfigurationManager.AppSettings["ClientSecret"];
                Scopes = ConfigurationManager.AppSettings["Scopes"].Split(',');
                TenentId = ConfigurationManager.AppSettings["TenentId"];

                MicrosoftLoginUrl = ConfigurationManager.AppSettings["MicrosoftLogin"];

                GraphClient = new GraphClient();

                RandomNumber = new Random();
            }
            catch (Exception ex)
            {
                // Log the exception details
                Console.WriteLine("Static initialization error: " + ex.Message);
                throw;
            }
        }

        public static string NpgSqlConnectionString { get; private set; }
        public static string SqlConnectionString { get; private set; }

        public static string ClientId { get; private set; }
        public static string ClientSecret { get; private set; }
        public static string[] Scopes { get; private set; }
        public static string TenentId { get; private set; }

        public static string MicrosoftLoginUrl { get; private set; }

        public static GraphClient GraphClient { get; private set; }

        public static Random RandomNumber { get; set; }
    }

}

