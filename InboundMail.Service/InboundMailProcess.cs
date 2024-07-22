using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Linq;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;
using Npgsql.Internal;
using iTextSharp.text.pdf;
using iTextSharp.text;
using iTextSharp.text.pdf.parser;
using System.IO;
using static System.Net.Mime.MediaTypeNames;
using System.Text;
using System.Net;
using System.Configuration;
using EmamiInboundMail.Service.Static;
using EmamiInboundMail.Service.Models;
using EmamiInboundMail.Service.Helper;

namespace EmamiInboundMail.Service
{
    public class InboundMailProcess
    {
        public static List<ProfitCenterConfig> ProfitCenterConfigList { get; set; }
        public static List<PlantConfig> PlantConfigList { get; set; }
        public static FtpConfig FtpConfiguration { get; set; }
        public static MailData MailData { get; set; }

        static readonly object _MailLock = new object();
        public InboundMailProcess()
        {
        }

        public static async Task Start()
        {
            try
            {
                lock (_MailLock)
                {
                    ErrorLog.WriteErrorLog(new string('*', 20) + "INBOUND MAIL PROCESS STARTED" + new string('*', 20));

                    GetAllProfitCenterConfig();
                    GetAllPlantConfig();
                    ReadAllConfigurationsAsync().Wait();

                    ErrorLog.WriteErrorLog(new string('*', 20) + "INBOUND MAIL PROCESS ENDED" + new string('*', 20));
                }

            }
            catch (Exception ex)
            {
                ErrorLog.WriteErrorLog("InboundMailProcess/Start/Exception : " + ex.Message);
                ErrorLog.WriteErrorLog(new string('*', 20) + "INBOUND MAIL PROCESS ENDED" + new string('*', 20));
            }
        }



        #region Read configuration

        public static void GetAllProfitCenterConfig()
        {
            try
            {
                ProfitCenterConfigList = new List<ProfitCenterConfig>();
                ErrorLog.WriteErrorLog("Getting Profit Center Configurations from Database");
                DataTable ProfitCenterDt = new DataTable();
                if (ConfigurationManager.AppSettings["Sql"] == "Sql")
                {
                    ProfitCenterDt = SqlHelper.ExecuteStoredProcedure("GetAllProfitCenteConfig");
                }
                else
                {
                    ProfitCenterDt = NpgSqlHelper.ExecuteStoredProcedure("GetAllProfitCenteConfig");
                }

                if (ProfitCenterDt.Rows.Count > 0)
                {
                    foreach (DataRow dr in ProfitCenterDt.Rows)
                    {
                        ProfitCenterConfigList.Add(new ProfitCenterConfig()
                        {
                            ID = int.Parse(dr["ID"].ToString()),
                            ProfitCenter = dr["ProfitCentre"].ToString(),
                            EmailID = dr["EmailID"].ToString(),
                            IsDeleted = string.IsNullOrEmpty(dr["IsDeleted"].ToString()) ? false : bool.Parse(dr["IsDeleted"].ToString()),
                        });
                    }
                    ErrorLog.WriteErrorLog($"Totally {ProfitCenterConfigList.Count} Profit Center Configurations found");
                }
            }
            catch (Exception ex)
            {
                ErrorLog.WriteErrorLog("InboundMailProcess/GetAllProfitCenterConfig/Exception :- " + ex.Message);
            }
        }

        public static void GetAllPlantConfig()
        {
            try
            {
                PlantConfigList = new List<PlantConfig>();
                ErrorLog.WriteErrorLog("Getting Plant Configurations from Database");

                DataTable PlantConfigDt = new DataTable();
                if (ConfigurationManager.AppSettings["Sql"] == "Sql")
                {
                    PlantConfigDt = SqlHelper.ExecuteStoredProcedure("GetAllPlantConfig");
                }
                else
                {
                    PlantConfigDt = NpgSqlHelper.ExecuteStoredProcedure("GetAllPlantConfig");
                }

                if (PlantConfigDt.Rows.Count > 0)
                {
                    foreach (DataRow dr in PlantConfigDt.Rows)
                    {
                        PlantConfigList.Add(new PlantConfig()
                        {
                            ID = int.Parse(dr["ID"].ToString()),
                            PlantCode = dr["PlantCode"].ToString(),
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorLog.WriteErrorLog("InboundMailProcess/GetAllPlantConfig/Exception :- " + ex.Message);
            }
        }

        #endregion


        #region Reading mail and attachments

        public static async Task ReadAllConfigurationsAsync()
        {
            try
            {
                ErrorLog.WriteErrorLog("Getting Configurations from Database");
                DataTable dtConfig = new DataTable();
                if (ConfigurationManager.AppSettings["Sql"] == "Sql")
                {
                    dtConfig = SqlHelper.ExecuteStoredProcedure("GetConfigurations");
                }
                else
                {
                    dtConfig = NpgSqlHelper.ExecuteStoredProcedure("GetConfigurations");
                }

                if (dtConfig.Rows.Count > 0)
                {
                    ErrorLog.WriteErrorLog($"Totally {dtConfig.Rows.Count} Configurations found");
                    FtpConfiguration = new FtpConfig();
                    foreach (DataRow dr in dtConfig.Rows)
                    {
                        FtpConfiguration.Email = dr["EmailID"].ToString();
                        FtpConfiguration.FtpUrl = dr["FTPUrl"].ToString();
                        FtpConfiguration.FtpUsername = dr["FTPUserName"].ToString();
                        FtpConfiguration.FtpPassword = dr["FTPPassword"].ToString();
                        FtpConfiguration.MarkAsread = Convert.ToBoolean(dr["MarkEmailAsRead"].ToString());
                        FtpConfiguration.DeleteAfterRead = Convert.ToBoolean(dr["MarkEmaiAsDeleted"].ToString());
                        FtpConfiguration.Password = dr["EmailPassword"].ToString();
                        FtpConfiguration.FileFormats = dr["FileFormatsAllowed"].ToString().Split(';');
                        FtpConfiguration.FolderPath = dr["FileLocation"].ToString();
                        FtpConfiguration.Plant = dr["Plant"].ToString();
                        FtpConfiguration.IncludeOriginalFileName = Convert.ToBoolean(dr["IncludeOriginalFileName"].ToString());
                        await ReadGraphAPIMailsAsync(FtpConfiguration.Email);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorLog.WriteErrorLog("InboundMailProcess/ReadAllConfigurations/Exceptoin :- " + ex.Message);
            }
        }

        public static async Task ReadGraphAPIMailsAsync(string ObjectId)
        {
            try
            {
                ErrorLog.WriteErrorLog("Starting to read mails using graph api.");
                string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Inbox");
                if (!System.IO.Directory.Exists(path))
                {
                    System.IO.Directory.CreateDirectory(path);
                }
                var client = StaticVariables.GraphClient.GetAuthenticatedClient();
                ErrorLog.WriteErrorLog("Successfully Read User Details");
                ErrorLog.WriteErrorLog("ClientResult :- " + client.Users.ToString());

                IUserMessagesCollectionPage msgs = await client.Users[ObjectId].Messages.Request()
               .Filter("isRead eq false")
               .GetAsync();
                List<Message> messages = new List<Message>();
                messages.AddRange(msgs.CurrentPage);
                while (msgs.NextPageRequest != null)
                {
                    msgs = await msgs.NextPageRequest.GetAsync();
                    messages.AddRange(msgs.CurrentPage);
                }
                ErrorLog.WriteErrorLog("Total messages count :- " + messages.Count);
                foreach (var item in messages)
                {
                    HtmlDocument doc = new HtmlDocument();
                    ErrorLog.WriteErrorLog("Mesage Count :- " + messages.Count + " & Read Status :- " + item.IsRead);
                    var body = StripHTML(item.Body.Content);
                    if (string.IsNullOrEmpty(body))
                    {
                        doc.LoadHtml(item.Body.Content);
                        HtmlNode[] nodes = doc.DocumentNode.SelectNodes("//div").ToArray();
                        foreach (HtmlNode items in nodes)
                        {
                            HtmlNode[] SPNode = items.SelectNodes("//span").ToArray();
                            foreach (HtmlNode Pitem in SPNode)
                            {
                                ErrorLog.WriteErrorLog("Item adding to Body " + Pitem.InnerText);
                                body = body + "\n" + Pitem.InnerText;
                            }
                        }
                    }
                    body = GetValuesFromHTMLBody(body);
                    ErrorLog.WriteErrorLog(new string('*', 5) + "BODY" + new string('*', 5));
                    ErrorLog.WriteErrorLog(body);
                    ErrorLog.WriteErrorLog(new string('*', 5) + "BODY" + new string('*', 5));
                    bool bodyValidation = BodyValidation(body);
                    bool fromAddressValidation = FromAddressValidation(item.From.EmailAddress.Address.Split('@')[1]);

                    MailData = new MailData();
                    MailData.FromAddress = item.From.EmailAddress.Address;
                    MailData.Subject = item.Subject;
                    MailData.MailDate = item.SentDateTime.Value.DateTime;

                    if ((bool)item.HasAttachments && bodyValidation && fromAddressValidation)
                    {
                        var attachmentDetails = await client.Users[ObjectId].Messages[item.Id].Attachments.Request().GetAsync();
                        var contentBytes = await client.Users[ObjectId].Messages[item.Id].Attachments[attachmentDetails.CurrentPage[0].Id].Request().GetAsync();
                        var content = (contentBytes as FileAttachment).ContentBytes;
                        var contentType = attachmentDetails.CurrentPage[0].ContentType;
                        var filePath = System.IO.Path.Combine(path, attachmentDetails.CurrentPage[0].Name);
                        ErrorLog.WriteErrorLog("Writting files into local inbox folder");
                        System.IO.File.WriteAllBytes(filePath, content);
                        ErrorLog.WriteErrorLog("Filename :- " + filePath);
                        ExtractOcrAndUploadFtp(filePath, contentType, body);
                        System.IO.File.Delete(filePath);
                        ErrorLog.WriteErrorLog("File deleted from local inbox folder" + filePath);
                        item.IsRead = true;
                        await client.Users[ObjectId].Messages[item.Id].Request().Select("IsRead").UpdateAsync(new Microsoft.Graph.Message { IsRead = true });
                        ErrorLog.WriteErrorLog("Marking message as seen with validation");
                    }
                    else
                    {
                        item.IsRead = true;
                        ErrorLog.WriteErrorLog("Marking message as seen without validation");
                        await client.Users[ObjectId].Messages[item.Id].Request().Select("IsRead").UpdateAsync(new Message { IsRead = true });
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorLog.WriteErrorLog("InboundMailProcess/ReadGraphAPIMailsAsync/Exception :- " + ex.Message);
            }
        }

        public static void ExtractOcrAndUploadFtp(string invoicePath, string fileType, string body)
        {
            if (fileType == "application/pdf")
            {
                ErrorLog.WriteErrorLog("Fetch Path is:- " + invoicePath);
                var runningNo = StaticVariables.RandomNumber.Next();
                ExtractTextFromPdf(invoicePath, DateTime.Now.ToString("yyyyMMddHHmmssfff") + "_" + runningNo, body);
                UploadFileToFTPFromRunningNumber(runningNo);
            }
            else
            {
                ErrorLog.WriteErrorLog("No pdf file. So, Marked message as seen.");
            }
        }

        public static bool ExtractTextFromPdf(string path, string FileName, string body)
        {
            StringBuilder text = new StringBuilder();
            string Extension = System.IO.Path.GetExtension(path);
            Dictionary<int, string> CurrentPDFTexts = new Dictionary<int, string>();
            try
            {
                ErrorLog.WriteErrorLog(Environment.NewLine);
                ErrorLog.WriteErrorLog("ExtractTextFromPdf : Reading File from : " + path);
                ErrorLog.WriteErrorLog(Environment.NewLine);
                using (PdfReader reader = new PdfReader(path))
                {
                    for (int i = 1; i <= reader.NumberOfPages; i++)
                    {
                        ErrorLog.WriteErrorLog("ExtractTextFromPdf :Reading Page NO " + i);
                        string PageText = PdfTextExtractor.GetTextFromPage(reader, i);
                        ErrorLog.WriteErrorLog("            PageText          \n" + PageText);

                        if (!string.IsNullOrEmpty(PageText))
                        {
                            if (MailData.Subject.StartsWith("9") || MailData.Subject.StartsWith("1"))
                            {
                                PageText = "GRN:-" + MailData.Subject + "\n" + "EmailId:-" + MailData.FromAddress + "\n" + "EmailDate:-" + MailData.MailDate.Date.ToString("dd/MM/yyyy") + "\n" + "EmailTime:-" + MailData.MailDate.ToString("HH:mm:ss") + "\n" + "ScannedDate:-" + DateTime.Now.ToString("dd/MM/yyy") + "\n" + "ScannedTime:-" + DateTime.Now.ToString("HH:mm:ss") + "\n" + PageText + "\n" + "Image Quality: 1";
                            }
                            else if (MailData.Subject.StartsWith("2"))
                            {
                                PageText = "FileName:-" + MailData.Subject + "\n" + "EmailId:-" + MailData.FromAddress + "\n" + "Store:-Acc" + "\n" + "EmailDate:-" + MailData.MailDate.ToString("dd/MM/yyyy") + "\n" + "EmailTime:-" + MailData.MailDate.ToString("HH:mm:ss") + "\n" + "ScannedDate:-" + DateTime.Now.ToString("dd/MM/yyy") + "\n" + "ScannedTime:-" + DateTime.Now.ToString("HH:mm:ss") + "\n" + PageText + "\n" + "Image Quality: 1";
                            }
                            else
                            {
                                PageText = "Store:-" + MailData.Subject + "\n" + "EmailId:-" + MailData.FromAddress + "\n" + "EmailDate:-" + MailData.MailDate.ToString("dd/MM/yyyy") + "\n" + "EmailTime:-" + MailData.MailDate.ToString("HH:mm:ss") + "\n" + "ScannedDate:-" + DateTime.Now.ToString("dd/MM/yyy") + "\n" + "ScannedTime:-" + DateTime.Now.ToString("HH:mm:ss") + "\n" + "-------------------------------------Indexed Text-----------------------------------------------" + "\n" + body + "\n" + "-------------------------------------OCR Text-----------------------------------------------" + "\n" + PageText + "\n" + "Image Quality: 1";
                            }
                            CurrentPDFTexts.Add(i, PageText);
                        }

                        text.Append(PageText);
                    }

                    string NewFileName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Inbox") + "\\" + MailData.Subject + FileName + "_0" + Extension;
                    System.IO.File.Copy(path, NewFileName);
                    string[] lines = text.ToString().Split(new string[] { "\n", }, StringSplitOptions.RemoveEmptyEntries);
                    string OcrText = string.Empty;
                    foreach (var line in lines)
                    {
                        if (!string.IsNullOrEmpty(line.Trim()))
                        {
                            OcrText = OcrText + line + Environment.NewLine;
                        }
                    }
                    System.IO.File.WriteAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Inbox") + "\\" + MailData.Subject + FileName + "_0" + ".txt", OcrText);
                }
                return true;
            }
            catch (Exception ex)
            {
                ErrorLog.WriteErrorLog("ExtractTextFromPdf/Exception :- " + ex.Message);
                return false;
            }
        }

        public static bool UploadFileToFTPFromRunningNumber(int runningNumber)
        {
            try
            {
                bool ReturnStatus = false;
                string[] filePaths = System.IO.Directory.GetFiles(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Inbox"), "*_" + runningNumber + "_*" + ".*");
                foreach (string file in filePaths)
                {
                    string TextFile = System.IO.Path.GetDirectoryName(file) + "\\" + System.IO.Path.GetFileNameWithoutExtension(file) + ".txt";
                    if (System.IO.Path.GetExtension(file) != ".txt")
                    {
                        if (filePaths.Any(TextFile.Contains))
                        {
                            bool Status = UplaodFileToFTP(file, TextFile);
                            if (Status)
                            {
                                ReturnStatus = true;
                            }
                            else
                            {
                                ErrorLog.WriteErrorLog("UploadFileToFTPFromRunningNumber:Failed To Uplaod FTP - " + file);
                            }
                        }
                        else
                        {
                            ErrorLog.WriteErrorLog("UploadFileToFTPFromRunningNumber:TextFile Not Present " + file);
                        }
                    }
                }
                return ReturnStatus;
            }
            catch (Exception ex)
            {
                ErrorLog.WriteErrorLog("UploadFileToFTPFromRunningNumber/Exception : " + ex.Message);
                return false;
            }
        }

        #endregion


        #region Additional Functions

        private static string StripHTML(string source)
        {
            try
            {
                string result;
                result = source.Replace("\r", " ");
                result = result.Replace("\n", " ");
                result = result.Replace("\t", string.Empty);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                                                                      @"( )+", " ");
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"<( )*head([^>])*>", "<head>",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"(<( )*(/)( )*head( )*>)", "</head>",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         "(<head>).*(</head>)", string.Empty,
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"<( )*script([^>])*>", "<script>",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"(<( )*(/)( )*script( )*>)", "</script>",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"(<script>).*(</script>)", string.Empty,
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"<( )*style([^>])*>", "<style>",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"(<( )*(/)( )*style( )*>)", "</style>",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         "(<style>).*(</style>)", string.Empty,
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"<( )*td([^>])*>", "\t",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"<( )*br( )*>", "\r",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"<( )*li( )*>", "\r",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"<( )*div([^>])*>", "\r\r",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"<( )*tr([^>])*>", "\r\r",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"<( )*p([^>])*>", "\r\r",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"<[^>]*>", string.Empty,
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @" ", " ",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"&bull;", " * ",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"&lsaquo;", "<",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"&rsaquo;", ">",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"&trade;", "(tm)",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"&frasl;", "/",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"&lt;", "<",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"&gt;", ">",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"&copy;", "(c)",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"&reg;", "(r)",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         @"&(.{2,6});", string.Empty,
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = result.Replace("\n", "\r");
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         "(\r)( )+(\r)", "\r\r",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         "(\t)( )+(\t)", "\t\t",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         "(\t)( )+(\r)", "\t\r",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         "(\r)( )+(\t)", "\r\t",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         "(\r)(\t)+(\r)", "\r\r",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                result = System.Text.RegularExpressions.Regex.Replace(result,
                         "(\r)(\t)+", "\r\t",
                         System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                string breaks = "\r\r\r";
                string tabs = "\t\t\t\t\t";
                for (int index = 0; index < result.Length; index++)
                {
                    result = result.Replace(breaks, "\r\r");
                    result = result.Replace(tabs, "\t\t\t\t");
                    breaks = breaks + "\r";
                    tabs = tabs + "\t";
                }
                return result;
            }
            catch (Exception ex)
            {
                ErrorLog.WriteErrorLog("StripHTML/Exception :- " + ex.Message);
                return source;
            }
        }

        public static string GetValuesFromHTMLBody(string HtmlBody)
        {
            try
            {
                string vendorKey = "Vendor:-";
                string invKey = "InvoiceNo:-";
                string invDateKey = "InvoiceDate:-";
                string AmountKey = "Amount:-";

                int venIndex = HtmlBody.IndexOf(vendorKey);

                venIndex += vendorKey.Length;
                int inIndex = HtmlBody.IndexOf(invKey);
                inIndex += invKey.Length;
                int idateIndex = HtmlBody.IndexOf(invDateKey);
                idateIndex += invDateKey.Length;
                int amIndex = HtmlBody.IndexOf(AmountKey);
                amIndex += AmountKey.Length;
                string Body = "";
                try
                {
                    Body = (HtmlBody.Substring(0, HtmlBody.IndexOf(vendorKey)));
                    Body = Body + "\n" + "Vendor:-" + (HtmlBody.Substring(venIndex, HtmlBody.IndexOf(invKey) - venIndex));
                    Body = Body + "\n" + "InvoiceNo:-" + (HtmlBody.Substring(inIndex, HtmlBody.IndexOf(invDateKey) - inIndex));
                    Body = Body + "\n" + "InvoiceDate:-" + (HtmlBody.Substring(idateIndex, HtmlBody.IndexOf(AmountKey) - idateIndex));
                    Body = Body + "\n" + "Amount:-" + (HtmlBody.Substring(amIndex, HtmlBody.Length - amIndex));
                }
                catch (Exception ex)
                {
                    Body = (HtmlBody.Substring(0, HtmlBody.IndexOf(invKey)));
                    Body = Body + "\n" + "InvoiceNo:-" + (HtmlBody.Substring(inIndex, HtmlBody.IndexOf(invDateKey) - inIndex));
                    Body = Body + "\n" + "InvoiceDate:-" + (HtmlBody.Substring(idateIndex, HtmlBody.IndexOf(AmountKey) - idateIndex));
                    Body = Body + "\n" + "Amount:-" + (HtmlBody.Substring(amIndex, HtmlBody.Length - amIndex));

                    ErrorLog.WriteErrorLog("GetValuesFromHTMLBody/Exception :- " + ex.Message);
                }

                return Body;
            }
            catch (Exception ex)
            {
                ErrorLog.WriteErrorLog("InboundMailProcess/GetValuesFromHTMLBody/Exception :- " + ex.Message);
                return HtmlBody;
            }
        }

        public static bool BodyValidation(string body)
        {
            if (ConfigurationManager.AppSettings["IsBodyValidationRequired"] == "Y")
            {
                return (body != null && body.Contains("##") && !(body.Contains("Thanks") || body.Contains("Regards")));
            }
            else
            {
                return true;
            }
        }

        public static bool FromAddressValidation(string fromAddress)
        {
            
            if (ConfigurationManager.AppSettings["IsFromAddressValidationRequired"] == "Y")
            {
                var fromAddresses = ConfigurationManager.AppSettings["FromAddresses"].Split(',');
                return fromAddresses.Contains(fromAddress);
            }
            else
            {
                return true;
            }
        }

        public static bool UplaodFileToFTP(string FileName, string TextFile)
        {
            bool IsFileUploaded = false;
            bool IsTextFileUploaded = false;
            try
            {
                string text = System.IO.File.ReadAllText(TextFile);
                if (!string.IsNullOrEmpty(text))
                {
                    ErrorLog.WriteErrorLog("UplaodFileToFTP:- TextFile Found..!!!");
                    if (System.IO.File.Exists(FileName))
                    {
                        ErrorLog.WriteErrorLog("UplaodFileToFTP:FileExists: " + FileName);
                        using (WebClient client = new WebClient())
                        {
                            string name = System.IO.Path.GetFileName(FileName);
                            client.Credentials = new NetworkCredential(FtpConfiguration.FtpUsername, FtpConfiguration.FtpPassword);
                            byte[] responseArray = client.UploadFile(FtpConfiguration.FtpUrl + name, FileName);
                            IsFileUploaded = true;
                        }
                    }

                    if (System.IO.File.Exists(TextFile))
                    {
                        ErrorLog.WriteErrorLog("UplaodFileToFTP:TextFileExists: " + TextFile);
                        using (WebClient client = new WebClient())
                        {
                            string name = System.IO.Path.GetFileName(TextFile);
                            client.Credentials = new NetworkCredential(FtpConfiguration.FtpUsername, FtpConfiguration.FtpPassword);
                            byte[] responseArray = client.UploadFile(FtpConfiguration.FtpUrl + name, TextFile);
                            IsTextFileUploaded = true;
                        }
                    }

                    if (IsFileUploaded)
                    {
                        if (System.IO.File.Exists(FileName))
                        {
                            try
                            {
                                System.IO.File.Delete(FileName);
                            }
                            catch (Exception ex)
                            {
                                ErrorLog.WriteErrorLog("UplaodFileToFTP:DeletingFile/Exception : " + ex.Message);
                            }
                        }
                    }

                    if (IsTextFileUploaded)
                    {
                        if (System.IO.File.Exists(TextFile))
                        {
                            try
                            {
                                System.IO.File.Delete(TextFile);
                            }
                            catch (Exception ex)
                            {
                                ErrorLog.WriteErrorLog("UplaodFileToFTP:DeletingTextFile/Exception : " + ex.Message);
                            }
                        }
                    }
                    if (IsFileUploaded && IsTextFileUploaded)
                    {
                        ErrorLog.WriteErrorLog(string.Format("Both File And TextFile Uploaded to ftp {0} :True ", FtpConfiguration.FtpUrl));
                        return true;
                    }
                    else
                    {
                        ErrorLog.WriteErrorLog(string.Format("Both File And TextFile Uploaded to ftp {0} :False ", FtpConfiguration.FtpUrl));
                        return false;
                    }
                }
                else
                {
                    ErrorLog.WriteErrorLog("0KB files are generated");
                    if (System.IO.File.Exists(FileName))
                    {
                        try
                        {
                            System.IO.File.Delete(FileName);
                        }
                        catch (Exception ex)
                        {
                            ErrorLog.WriteErrorLog("UplaodFileToFTP:DeletingFile/Exception " + ex.Message);
                        }
                    }
                    if (System.IO.File.Exists(TextFile))
                    {
                        try
                        {
                            System.IO.File.Delete(TextFile);
                        }
                        catch (Exception ex)
                        {
                            ErrorLog.WriteErrorLog("UplaodFileToFTP:DeletingTextFile/Exception " + ex.Message);
                        }
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorLog.WriteErrorLog("UplaodFileToFTP/Exception : " + ex.Message);
                return false;
            }
        }

        #endregion
    }
}
