using System;
using System.Collections.Generic;
using System.Text;

using System.Web;
using System.Net;
using System.IO;
using System.Runtime.Serialization.Json;

namespace GoogleTranslator
{

   public class GoogleLangaugeDetector
   {
      private string _q = "";
      private string _v = "";
      private string _key = "";
      private string _requestUrl = "";
      private string _languageDetected = "";

      public GoogleLangaugeDetector(string queryTerm, VERSION version, string key)
      {
         _q = HttpUtility.UrlPathEncode(queryTerm);
         _v = HttpUtility.UrlEncode(EnumStringUtil.GetStringValue(version));
         _key = HttpUtility.UrlEncode(key);

         string encodedRequestUrlFragment = 
            string.Format("?v={0}&q={1}&key={2}",
            _v, _q, _key);

         _requestUrl = EnumStringUtil.GetStringValue(BASEURL.DETECT) + encodedRequestUrlFragment;

         DetectLanguage();
      }

      public string LanguageDetected
      {
         get { return _languageDetected; }
         private set { _languageDetected = value; }
      }

      private void DetectLanguage()
      {
         try
         {
            WebRequest request = WebRequest.Create(_requestUrl);
            WebResponse response = request.GetResponse();

            StreamReader reader = new StreamReader(response.GetResponseStream());
            string json = reader.ReadLine();
            using (MemoryStream ms = new MemoryStream(Encoding.Unicode.GetBytes(json)))
            {
               DataContractJsonSerializer ser =
                  new DataContractJsonSerializer(typeof(LanguageDetector));
               LanguageDetector _detector = ser.ReadObject(ms) as LanguageDetector;

               _languageDetected = _detector.responseData.language;
            }
         }
         catch (Exception) { }
      }
     
   }
}
