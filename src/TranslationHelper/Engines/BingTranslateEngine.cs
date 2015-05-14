using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Script.Serialization;
using TranslationHelper.Infos;

namespace TranslationHelper.Engines
{
    public class BingTranslateEngine : ITranslateEngine
    {
        private const string englishCulture = "en";
        private const string bingUrlFormat = "http://api.microsofttranslator.com/v2/ajax.svc/TranslateArray2?appId=%22T2gRWJdUpuqSW6U0s8nI73Ayyh2q4S5Z1dTYz9Dha1Xg*%22&texts=%5B%22{2}%22%5D&from=%22{0}%22&to=%22{1}%22";

        private JavaScriptSerializer javaScriptSerializer;

        public string FromCulture { get; set; }
        public string ToCulture { get; set; }

        public BingTranslateEngine()
        {
            FromCulture = englishCulture;
            ToCulture = englishCulture;
            
            javaScriptSerializer = new JavaScriptSerializer();
        }

        public string TranslateWordOrPhrase(string wordOrPhraseToTranslate)
        {
            var translatedValue = wordOrPhraseToTranslate;

            try
            {
                var url = String.Format(bingUrlFormat, FromCulture, ToCulture, HttpUtility.UrlEncode(wordOrPhraseToTranslate));
                var webReq = CreateTranslationRequest(url);
                using (var webResponse = webReq.GetResponse())
                {
                    using (var responseStream = webResponse.GetResponseStream())
                    {
                        if (responseStream == null)
                            throw new Exception("No response stream found for the given url");

                        var streamReader = new StreamReader(responseStream, System.Text.Encoding.UTF8);
                        var responseData = streamReader.ReadToEnd();

                        translatedValue = GetTranslatedValueFromJson(responseData);
                    }
                }                
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Unable to translate due to the follow error.");
                Trace.WriteLine(ex);
                if (Debugger.IsAttached) Debugger.Break();
            }

            return translatedValue;
        }

        private HttpWebRequest CreateTranslationRequest(string url)
        {
            var webReq = (HttpWebRequest)WebRequest.Create(url);
            webReq.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            webReq.ContentType = "application/json";
            webReq.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:38.0) Gecko/20100101 Firefox/38.0";
            webReq.Referer = "https://www.bing.com/translator/";

            return webReq;
        }

        private string GetTranslatedValueFromJson(string page)
        {
            var json = javaScriptSerializer.Deserialize<BingTranslationResult[]>(page);
            if (json.Any())
                return json[0].TranslatedText;

            return null;
        }
    }
}
