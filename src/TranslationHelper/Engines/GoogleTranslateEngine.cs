using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Script.Serialization;

namespace TranslationHelper.Engines
{
    //  TODO: Currently the code provided does not allow for full Unicode translations such as Chinese characters, etc.
    public class GoogleTranslateEngine
    {
        private const string englishCulture = "en";
        //                                             http://translate.google.com/translate_a/t?client=webapp&sl=en&tl=ja&hl=en&q=Hold%20back%20the%20rain&sc=1
        private const string googleDefaultUrlFormat = "http://translate.google.com/translate_a/t?client=webapp&sl={0}&tl={1}&hl=en&q={2}&sc=1";

        private WebClient webClient;
        private JavaScriptSerializer javaScriptSerializer;

        public string FromCulture { get; set; }
        public string ToCulture { get; set; }
        public string GoogleTranslateUrl { get; set; }

        public GoogleTranslateEngine() : this(new WebClient()) { }
        
        public GoogleTranslateEngine(WebClient webClient)
        {
            FromCulture = englishCulture;
            ToCulture = englishCulture;
            GoogleTranslateUrl = googleDefaultUrlFormat;
            
            javaScriptSerializer = new JavaScriptSerializer();
        }

        public string TranslateWordOrPhrase(string wordOrPhraseToTranslate)
        {
            var translatedValue = wordOrPhraseToTranslate;

            try
            {
                var url = String.Format(GoogleTranslateUrl, FromCulture, ToCulture, HttpUtility.UrlEncode(wordOrPhraseToTranslate));

                var webReq = WebRequest.Create(url) as HttpWebRequest;
                if (webReq == null)
                    throw new Exception("Unable to create a web request from the given url");

                webReq.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
                webReq.UserAgent = "Opera/12.02 (Android 4.1; Linux; Opera Mobi/ADR-1111101157; U; en-US) Presto/2.9.201 Version/12.02";
                webReq.Referer = "http://translate.google.com/m/translate";

                var webResponse = webReq.GetResponse();
                using (var responseStream = webResponse.GetResponseStream())
                {
                    if (responseStream == null)
                        throw new Exception("No response stream found for the given url");

                    var streamReader = new StreamReader(responseStream, System.Text.Encoding.UTF8);
                    var responseData = streamReader.ReadToEnd();
                    
                    translatedValue = responseData.StartsWith("<!DOCTYPE html>")
                                            ? GetTranslatedValueFromWebPageData(responseData, wordOrPhraseToTranslate)
                                            : GetTranslatedValueFromJson(responseData);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Unable to translate due to the follow error.");
                Trace.WriteLine(ex);
                if (Debugger.IsAttached)
                    Debugger.Break();
            }

            return translatedValue;
        }

        private string GetTranslatedValueFromJson(string page)
        {
            var json = javaScriptSerializer.Deserialize<GoogleTranslationResult>(page);

            return json.sentences.First().trans;
        }

        private static string GetTranslatedValueFromWebPageData(string page, string value)
        {
            var resultBoxData = GetSpanValue(page, "result_box");
            var translatedValue = GetSpanValue(resultBoxData, value);
            
            translatedValue = translatedValue.Replace("<br>", String.Empty);
            translatedValue = HttpUtility.HtmlDecode(translatedValue);

            return translatedValue;
        }

        private static string GetSpanValue(string pageData, string idOrTitleValue)
        {
            var result = String.Empty;

            //  Get result Box
            var start = pageData.IndexOf("<span id=" + HttpUtility.HtmlEncode(idOrTitleValue));
            if (start == -1)
                start = pageData.IndexOf("<span title=\"" + HttpUtility.HtmlEncode(idOrTitleValue));
            start = pageData.IndexOf('>', start) + 1;
            var end = pageData.IndexOf("</span>", start);

            if (end > 0)
                result = pageData.Substring(start, end - start);
            else
                result = pageData.Substring(start);

            return result;
        }
    }
}
