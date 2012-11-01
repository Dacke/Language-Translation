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
    public class GoogleTranslateEngine
    {
        private const string englishCulture = "en";
        private const string googleUrlFormat = "http://translate.google.com/translate_a/t?client=webapp&sl={0}&tl={1}&hl=en&q={2}&sc=1";

        private JavaScriptSerializer javaScriptSerializer;

        public string FromCulture { get; set; }
        public string ToCulture { get; set; }

        public GoogleTranslateEngine()
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
                var url = String.Format(googleUrlFormat, FromCulture, ToCulture, HttpUtility.UrlEncode(wordOrPhraseToTranslate));
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
            webReq.UserAgent = "Opera/12.02 (Android 4.1; Linux; Opera Mobi/ADR-1111101157; U; en-US) Presto/2.9.201 Version/12.02";
            webReq.Referer = "http://translate.google.com/m/translate";

            return webReq;
        }

        private string GetTranslatedValueFromJson(string page)
        {
            var json = javaScriptSerializer.Deserialize<GoogleTranslationResult>(page);

            return json.sentences.First().trans;
        }
    }
}
