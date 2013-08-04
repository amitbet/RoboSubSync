using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Web;
using System.Net;
using System.Threading;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Xml.Linq;

namespace SyncSubsByComparison
{
    public class BingTranslator
    {
        static string _clientId = "c8016bfc-11fc-4905-8630-fd9bfd00e697";
        static string _clientSecret = "zRrUfDpO57x/9tnx16m/2oEpXzK6amgydzKi7tEBJXw";
        /// <summary>
        /// 
        /// </summary>
        /// <param name="text">the text to translate</param>
        /// <param name="targetLanguage">ar,bg,ca,zh-CHS,zh-CHT,cs,da,nl,en,et,fi,fr,de,el,ht,he,hi,mww,hu,id,it,ja,tlh,tlh-QON,ko,lv,lt,ms,no,fa,pl,pt,ro,ru,sk,sl,es,sv,th,tr,uk,ur,vi</param>
        /// <returns></returns>
        public static IEnumerable<string> TranslateArray(IEnumerable<string> lines, string targetLanguage, string sourceLanguage)
        {
            AdmAccessToken admToken;
            string headerValue;
            //Get Client Id and Client Secret from https://datamarket.azure.com/developer/applications/
            //Refer obtaining AccessToken (http://msdn.microsoft.com/en-us/library/hh454950.aspx) 
            //string clientId = "c8016bfc-11fc-4905-8630-fd9bfd00e697";
            //string clientSecret = "zRrUfDpO57x/9tnx16m/2oEpXzK6amgydzKi7tEBJXw";

            //var lines = text.Split("\n\r".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            //List<string> translationBuckets = new List<string>();
            //StringBuilder currentBucket = new StringBuilder();
            ////string currentBucket = "";
            //foreach (var line in lines)
            //{
            //    currentBucket.AppendLine(line);
            //    //currentBucket+=line.Length;
            //    if (currentBucket.Length > 2000)
            //    {
            //        translationBuckets.Add(currentBucket.ToString());
            //        currentBucket = new StringBuilder();
            //    }
            //}
            //string currentBucket = "";
            ////string currentBucket = "";
            //foreach (var line in lines)
            //{
            //    currentBucket += line + "\n";
            //    //currentBucket+=line.Length;
            //    if (currentBucket.Length > 2000)
            //    {
            //        translationBuckets.Add(currentBucket);
            //        currentBucket = "";
            //    }
            //}
            //AdmAccessToken admToken;
            //string headerValue;
            //Get Client Id and Client Secret from https://datamarket.azure.com/developer/applications/
            //Refer obtaining AccessToken (http://msdn.microsoft.com/en-us/library/hh454950.aspx) 
            //AdmAuthentication admAuth = new AdmAuthentication(clientId, clientSecret);
            try
            {
                //admToken = admAuth.GetAccessToken();
                // Create a header with the access_token property of the returned token
                //headerValue = "Bearer " + admToken.access_token;
                //return TranslateArrayMethod(headerValue, lines, targetLanguage, sourceLanguage);
                return TranslateArrayMethod(lines, targetLanguage, sourceLanguage);
            }


            //StringBuilder sbTranslation = new StringBuilder();
            //AdmAuthentication admAuth = new AdmAuthentication(clientId, clientSecret);
            //try
            //{
            //    //foreach (string bucket in translationBuckets)
            //    ////foreach (var line in lines)
            //    //{
            //    //    //admToken = admAuth.GetAccessToken();
            //    //    // Create a header with the access_token property of the returned token
            //    //    //headerValue = "Bearer " + admToken.access_token;
            //    //    BingTranslationAdapter adapter = new BingTranslationAdapter();
            //    //    //string translation = TranslateInternal(targetLanguage, headerValue, line);
            //    //    string translation = adapter.Translate(bucket, targetLanguage, sourceLanguage);
            //    //    sbTranslation.AppendLine(translation.Trim("\n\r".ToCharArray()));
            //    //}
            //    //return sbTranslation.ToString();

            //}
            catch (WebException e)
            {
                ProcessWebException(e);
            }
            return null;
        }

        private static IEnumerable<string> TranslateArrayMethod(IEnumerable<string> translateArraySourceTexts, string toLang, string fromLang = null)
        {
            //string from = "en";
            //string to = "es";
            //string[] translateArraySourceTexts = { "The answer lies in machine translation.", "the best machine translation technology cannot always provide translations tailored to a site or users like a human ", "Simply copy and paste a code snippet anywhere " };
            StringBuilder sb = new StringBuilder();
            //System.Uri.EscapeDataString(l)
            var translationLines = translateArraySourceTexts.Select(l => "<string xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\">" + System.Web.HttpUtility.HtmlEncode(l) + "</string>");

            var translationGroups = translationLines
            .Select((line, index) => new { line, index })
            .GroupBy(g => g.index / 50, i => i.line);

            List<string> batches = new List<string>();
            foreach (var group in translationGroups)
            {
                batches.Add(group.Aggregate((a, b) => a + "\n" + b));
            }
            List<string> translatedLines = new List<string>();
            foreach (string batch in batches)
            {
                AdmAccessToken admToken;
                AdmAuthentication admAuth = new AdmAuthentication(_clientId, _clientSecret);
                admToken = admAuth.GetAccessToken();
                // Create a header with the access_token property of the returned token
                string authToken = "Bearer " + admToken.access_token;

                string uri = "http://api.microsofttranslator.com/v2/Http.svc/TranslateArray";
                string body = "<TranslateArrayRequest>" +
                                 "<AppId />" +
                                 "<From>{0}</From>" +
                                 "<Options>" +
                                    " <Category xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.MT.Web.Service.V2\" />" +
                                     "<ContentType xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.MT.Web.Service.V2\">{1}</ContentType>" +
                                     "<ReservedFlags xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.MT.Web.Service.V2\" />" +
                                     "<State xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.MT.Web.Service.V2\" />" +
                                     "<Uri xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.MT.Web.Service.V2\" />" +
                                     "<User xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.MT.Web.Service.V2\" />" +
                                 "</Options>" +
                                 "<Texts>\n" +
                                    batch +
                    //"<string xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\">{2}</string>" +
                    //"<string xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\">{3}</string>" +
                    //"<string xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\">{4}</string>" +
                                 "</Texts>" +
                                 "<To>{2}</To>" +
                              "</TranslateArrayRequest>";
                string reqBody = string.Format(body, fromLang, "text/plain", toLang);
                // create the request
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
                request.Headers.Add("Authorization", authToken);
                request.ContentType = "text/xml";
                request.Method = "POST";

                using (System.IO.Stream stream = request.GetRequestStream())
                {
                    byte[] arrBytes = System.Text.Encoding.UTF8.GetBytes(reqBody);
                    stream.Write(arrBytes, 0, arrBytes.Length);
                }

                // Get the response
                WebResponse response = null;
                try
                {
                    response = request.GetResponse();
                    using (Stream stream = response.GetResponseStream())
                    {
                        using (StreamReader rdr = new StreamReader(stream, System.Text.Encoding.UTF8))
                        {
                            // Deserialize the response
                            string strResponse = rdr.ReadToEnd();
                            Console.WriteLine("Result of translate array method is:");
                            XDocument doc = XDocument.Parse(@strResponse);
                            XNamespace ns = "http://schemas.datacontract.org/2004/07/Microsoft.MT.Web.Service.V2";
                            int soureceTextCounter = 0;
                            foreach (XElement xe in doc.Descendants(ns + "TranslateArrayResponse"))
                            {

                                foreach (var node in xe.Elements(ns + "TranslatedText"))
                                {
                                    translatedLines.Add(node.Value);//Console.WriteLine("\n\nSource text: {0}\nTranslated Text: {1}", translateArraySourceTexts[soureceTextCounter], node.Value);
                                }
                                //soureceTextCounter++;
                            }
                            //Console.WriteLine("Press any key to continue...");
                            //Console.ReadKey(true);
                        }
                    }
                }
                catch
                {
                    throw;
                }
                finally
                {
                    if (response != null)
                    {
                        response.Close();
                        response = null;
                    }
                }
            }

            return translatedLines.ToArray();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="targetLang">ar,bg,ca,zh-CHS,zh-CHT,cs,da,nl,en,et,fi,fr,de,el,ht,he,hi,mww,hu,id,it,ja,tlh,tlh-QON,ko,lv,lt,ms,no,fa,pl,pt,ro,ru,sk,sl,es,sv,th,tr,uk,ur,vi</param>
        /// <param name="authToken"></param>
        /// <param name="textToTranslate"></param>
        /// <returns></returns>
        private static string TranslateInternal(string targetLang, string authToken, string textToTranslate)
        {
            //https://api.datamarket.azure.com/Bing/MicrosoftTranslator/v1/Translate?Text=%27%D7%9B%D7%A8%D7%92%D7%99%D7%9C%27&To=%27en%27
            //Console.WriteLine("Enter Text to detect language:");
            //string textToDetect = Console.ReadLine();
            //Keep appId parameter blank as we are sending access token in authorization header.
            //textToTranslate = "דרור אכל גלידה";
            //string uri = "https://api.datamarket.azure.com/Bing/MicrosoftTranslator/v1/Translate?text='" + textToTranslate + "'" + "&To='" + targetLang + "'";
            string uri = "http://api.microsofttranslator.com/v2/Http.svc/Translate?" + "to=" + targetLang + "&text=" + HttpUtility.UrlEncode(textToTranslate);
            //string uri = "http://api.microsofttranslator.com/v2/Http.svc/Translate?to=" + HttpUtility.UrlEncode(targetLang);

            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(uri);
            httpWebRequest.Headers.Add("Authorization", authToken);

            //httpWebRequest.Headers.Add("text", HttpUtility.UrlEncode(textToTranslate.Substring(0,1000)));
            //httpWebRequest.Headers.Add("text", HttpUtility.UrlEncode(textToTranslate.Substring(0, 9000)));

            WebResponse response = null;
            string translatedText;
            try
            {
                response = httpWebRequest.GetResponse();
                using (Stream stream = response.GetResponseStream())
                {
                    System.Runtime.Serialization.DataContractSerializer dcs = new System.Runtime.Serialization.DataContractSerializer(Type.GetType("System.String"));
                    translatedText = (string)dcs.ReadObject(stream);
                }
            }

            catch
            {
                throw;
            }
            finally
            {
                if (response != null)
                {
                    response.Close();
                    response = null;
                }
            }
            return translatedText;
        }

        public static string DetectLanguage(string text)
        {
            AdmAccessToken admToken;
            string headerValue;
            //Get Client Id and Client Secret from https://datamarket.azure.com/developer/applications/
            //Refer obtaining AccessToken (http://msdn.microsoft.com/en-us/library/hh454950.aspx) 
            string clientId = "c8016bfc-11fc-4905-8630-fd9bfd00e697";
            string clientSecret = "zRrUfDpO57x/9tnx16m/2oEpXzK6amgydzKi7tEBJXw";

            AdmAuthentication admAuth = new AdmAuthentication(clientId, clientSecret);
            try
            {
                admToken = admAuth.GetAccessToken();
                // Create a header with the access_token property of the returned token
                headerValue = "Bearer " + admToken.access_token;
                return DetectLangInternal(headerValue, text);
            }
            catch (WebException e)
            {
                ProcessWebException(e);
            }
            return null;
        }





        private static string DetectLangInternal(string authToken, string textToDetect)
        {
            //Console.WriteLine("Enter Text to detect language:");
            //string textToDetect = Console.ReadLine();
            //Keep appId parameter blank as we are sending access token in authorization header.
            string uri = "http://api.microsofttranslator.com/v2/Http.svc/Detect?text=" + textToDetect;
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(uri);
            httpWebRequest.Headers.Add("Authorization", authToken);
            WebResponse response = null;
            string languageDetected;
            try
            {
                response = httpWebRequest.GetResponse();
                using (Stream stream = response.GetResponseStream())
                {
                    System.Runtime.Serialization.DataContractSerializer dcs = new System.Runtime.Serialization.DataContractSerializer(Type.GetType("System.String"));
                    languageDetected = (string)dcs.ReadObject(stream);
                }

            }

            catch
            {
                throw;
            }
            finally
            {
                if (response != null)
                {
                    response.Close();
                    response = null;
                }
            }
            return languageDetected;
        }

        private static void ProcessWebException(WebException e)
        {
            Console.WriteLine("{0}", e.ToString());
            // Obtain detailed error information
            string strResponse = string.Empty;
            using (HttpWebResponse response = (HttpWebResponse)e.Response)
            {
                using (Stream responseStream = response.GetResponseStream())
                {
                    using (StreamReader sr = new StreamReader(responseStream, System.Text.Encoding.ASCII))
                    {
                        strResponse = sr.ReadToEnd();
                    }
                }
            }
            Console.WriteLine("Http status code={0}, error message={1}", e.Status, strResponse);
        }
    }
    [DataContract]
    public class AdmAccessToken
    {
        [DataMember]
        public string access_token { get; set; }
        [DataMember]
        public string token_type { get; set; }
        [DataMember]
        public string expires_in { get; set; }
        [DataMember]
        public string scope { get; set; }
    }

    public class AdmAuthentication
    {
        public static readonly string DatamarketAccessUri = "https://datamarket.accesscontrol.windows.net/v2/OAuth2-13";
        private string clientId = "";
        private string clientSecret = "";
        private string request;
        private AdmAccessToken token;
        private Timer accessTokenRenewer;

        //Access token expires every 10 minutes. Renew it every 9 minutes only.
        private const int RefreshTokenDuration = 9;

        public AdmAuthentication(string clientId, string clientSecret)
        {
            this.clientId = clientId;
            this.clientSecret = clientSecret;
            //If clientid or client secret has special characters, encode before sending request
            this.request = string.Format("grant_type=client_credentials&client_id={0}&client_secret={1}&scope=http://api.microsofttranslator.com", HttpUtility.UrlEncode(clientId), HttpUtility.UrlEncode(clientSecret));
            this.token = HttpPost(DatamarketAccessUri, this.request);
            //renew the token every specfied minutes
            accessTokenRenewer = new Timer(new TimerCallback(OnTokenExpiredCallback), this, TimeSpan.FromMinutes(RefreshTokenDuration), TimeSpan.FromMilliseconds(-1));
        }

        public AdmAccessToken GetAccessToken()
        {
            return this.token;
        }


        private void RenewAccessToken()
        {
            AdmAccessToken newAccessToken = HttpPost(DatamarketAccessUri, this.request);
            //swap the new token with old one
            //Note: the swap is thread unsafe
            this.token = newAccessToken;
            Console.WriteLine(string.Format("Renewed token for user: {0} is: {1}", this.clientId, this.token.access_token));
        }

        private void OnTokenExpiredCallback(object stateInfo)
        {
            try
            {
                RenewAccessToken();
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Failed renewing access token. Details: {0}", ex.Message));
            }
            finally
            {
                try
                {
                    accessTokenRenewer.Change(TimeSpan.FromMinutes(RefreshTokenDuration), TimeSpan.FromMilliseconds(-1));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.Format("Failed to reschedule the timer to renew access token. Details: {0}", ex.Message));
                }
            }
        }


        private AdmAccessToken HttpPost(string DatamarketAccessUri, string requestDetails)
        {
            //Prepare OAuth request 
            WebRequest webRequest = WebRequest.Create(DatamarketAccessUri);
            webRequest.ContentType = "application/x-www-form-urlencoded";
            webRequest.Method = "POST";
            byte[] bytes = Encoding.ASCII.GetBytes(requestDetails);
            webRequest.ContentLength = bytes.Length;
            using (Stream outputStream = webRequest.GetRequestStream())
            {
                outputStream.Write(bytes, 0, bytes.Length);
            }
            using (WebResponse webResponse = webRequest.GetResponse())
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(AdmAccessToken));
                //Get deserialized object from JSON stream
                AdmAccessToken token = (AdmAccessToken)serializer.ReadObject(webResponse.GetResponseStream());
                return token;
            }
        }
    }

}
