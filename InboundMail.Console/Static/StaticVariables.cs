using EmamiInboundMail.Console.Models;
using System;
using System.Collections.Generic;
using System.Configuration;

namespace EmamiInboundMail.Console.Static
{
    public static class StaticVariables
    {
        public static string NpgSqlConnectionString = ConfigurationManager.ConnectionStrings["NpgSqlAuthContext"].ConnectionString;
        public static string SqlConnectionString = ConfigurationManager.ConnectionStrings["SqlAuthContext"].ConnectionString;

        public static string ClientId = ConfigurationManager.AppSettings["ClientId"];
        public static string ClientSecret = ConfigurationManager.AppSettings["ClientSecret"];
        public static string[] Scopes = ConfigurationManager.AppSettings["Scopes"].Split(',');
        public static string TenentId = ConfigurationManager.AppSettings["TenentId"];

        public static string MicrosoftLoginUrl = ConfigurationManager.AppSettings["MicrosoftLogin"];

        public static List<ProfitCenterConfig> ProfitCenterConfigList = new List<ProfitCenterConfig>();
        public static List<PlantConfig> PlantConfigList = new List<PlantConfig>();
        public static FtpConfig FtpConfiguration = new FtpConfig();
        public static MailData MailData = new MailData();

        public static GraphClient GraphClient = new GraphClient();

        public static Random RandomNumber;
    }
}
