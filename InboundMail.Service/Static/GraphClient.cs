using Microsoft.Graph;
using System;
using Azure.Core;
using System.Net.Http.Headers;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;

namespace EmamiInboundMail.Service.Static
{
    public class GraphClient
    {
        private GraphServiceClient graphClient = null;
        // Get an access token for the given context and resourced. An attempt is first made to   
        // acquire the token silently. If that fails, then we try to acquire the token by prompting the user.  
        public GraphServiceClient GetAuthenticatedClient()
        {
            if (graphClient == null)
            {
                // Create Microsoft Graph client.  
                try
                {
                    graphClient = new GraphServiceClient(
                        "https://graph.microsoft.com/v1.0",
                        new DelegateAuthenticationProvider(
                            async (requestMessage) =>
                            {
                                var token = await GetAccessToken();
                                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", token);
                                // This header has been added to identify our sample in the Microsoft Graph service.  If extracting this code for your project please remove.  
                                requestMessage.Headers.Add("d040072c-4d97-4872-b780-b33d0af0136b", "InboundMail-AP");

                            }));
                    return graphClient;
                }

                catch (Exception ex)
                {
                    ErrorLog.WriteErrorLog("Could not create a graph client: " + ex.Message);
                }
            }

            return graphClient;
        }

        public async Task<string> GetAccessToken()
        {
            var client = new HttpClient();
            var url = $"{StaticVariables.MicrosoftLoginUrl}{StaticVariables.TenentId}/oauth2/v2.0/token";
            var values = new Dictionary<string, string>
                {
                    { "client_id", StaticVariables.ClientId },
                    { "client_secret", StaticVariables.ClientSecret },
                    { "scope",@"https://graph.microsoft.com/.default"},
                    { "grant_type", "client_credentials" }
                };


            var content = new FormUrlEncodedContent(values);
            var response = await client.PostAsync(url, content);
            var responseString = await response.Content.ReadAsStringAsync();
            dynamic data = JObject.Parse(responseString);
            return data.access_token;
        }
    
    }
}
