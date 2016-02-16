using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Newtonsoft.Json.Linq;
using AngularJSAuthentication.API.Models;
using Newtonsoft.Json;

namespace AngularJSAuthentication.API.Controllers
{
    [RoutePrefix("api/Contacts")]
    public class ContactsController : ApiController
    {
        // POST api/Contacts/GoogleLoad
        [Authorize]
        [Route("GoogleLoad")]
        public async Task<IHttpActionResult> GoogleLoad(RegisterExternalBindingModel model)
        {
            string resultJson = string.Empty;
            string contactsTokenEndPoint = string.Format(@"https://www.google.com/m8/feeds/contacts/default/full?alt=json&access_token={0}&max-results=500&v=3.0", model.ExternalAccessToken);

            List<GMailContactModel> gmailContacts = new List<GMailContactModel>();

            try
            {
                var client = new HttpClient();
                var request = new HttpRequestMessage()
                {
                    RequestUri = new Uri(contactsTokenEndPoint),
                    Method = HttpMethod.Get
                };

                request.Headers.Add("Origin", "http://localhost:32150");
                request.Headers.Accept.Clear();
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
                request.Headers.Referrer = new Uri("http://localhost:32150/TestContacts.html");
                request.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/48.0.2564.109 Safari/537.36");
                request.Headers.Add("x-client-data", "CKO2yQEIxLbJAQj9lcoB");
                request.Headers.Add("cache-control", "no-cache");
                request.Headers.Add("accept-language", "en-US,en;q=0.8");
                request.Headers.Add("accept-encoding", "gzip, deflate, sdch");

                var task = client.SendAsync(request)
                   .ContinueWith((taskwithmsg) =>
                    {
                        var response = taskwithmsg.Result;
 
                        if (response.Content.Headers.ContentEncoding.ToString().ToLower().Contains("gzip"))
                        {
                            var responseStream = new GZipStream(response.Content.ReadAsStreamAsync().Result, CompressionMode.Decompress);

                            StreamReader reader = new StreamReader(responseStream, Encoding.UTF8);

                            resultJson = reader.ReadToEnd();
                        }
                        else
                        {
                            resultJson = HandleChankedContent(response);
                        }
                    });
                task.Wait();

                var entries = Newtonsoft.Json.JsonConvert.DeserializeObject<JObject>(resultJson)["feed"]["entry"] as JArray;
                
                foreach (var entry in entries)
                {
                    Debug.WriteLine(" next entry processing " + entries.IndexOf(entry));

                    if (entry["gd$email"] == null)
                    {
                        continue;
                    }

                    var emailNode = entry["gd$email"].FirstOrDefault(ml => ml["primary"] != null && ml["primary"].Value<string>() == "true");
                    if (emailNode == null && entry["gd$email"].Any())
                    {
                        emailNode = entry["gd$email"].First();
                    }

                    if (emailNode == null || string.IsNullOrEmpty(emailNode["address"].Value<string>()))
                    {
                        continue;    
                    }
                    
                    string email = emailNode["address"].Value<string>();
                    
                    if (string.IsNullOrEmpty(email))
                    {
                        continue;
                    }

                    string name = entry["title"].HasValues ? entry["title"]["$t"].Value<string>() : "";
                    if (string.IsNullOrEmpty(name))
                    {
                        name = email;
                    }
                    string avaurl = entry["link"].HasValues ? entry["link"][0]["href"].Value<string>() : "";
                    if (!string.IsNullOrEmpty(avaurl))
                    {
                        avaurl += "&access_token=" + model.ExternalAccessToken;
                    }

                    gmailContacts.Add(new GMailContactModel()
                    {
                        email = email,
                        name = name,
                        avaurl = avaurl
                    });
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }

            return Ok(gmailContacts);
        }

        private static string HandleChankedContent(HttpResponseMessage response)
        {
            StringBuilder sb = new StringBuilder();
            Byte[] buf = new byte[8192];
            Stream resStream = response.Content.ReadAsStreamAsync().Result;
            string tmpString = null;
            int count = 0;
            do
            {
                count = resStream.Read(buf, 0, buf.Length);
                if (count != 0)
                {
                    tmpString = Encoding.UTF8.GetString(buf, 0, count);
                    sb.Append(tmpString);
                }
            } while (count > 0);
            return tmpString;
        }
    }
}
