using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;

namespace TranslationHelper
{
    //  TODO: Currently the code provided does not allow for full Unicode translations such as Chinese characters, etc.
    class GoogleTranslateHelper
    {
        private const string englishCulture = "en";
        private const string googleDefaultUrlFormat = "http://translate.google.com/?sl={0}&tl={1}&js=n&prev=_t&hl=en&layout=2&eotf=1&text={2}";

        public string FromCulture { get; set; }
        public string ToCulture { get; set; }
        public string GoogleTranslateUrl { get; set; }

        public GoogleTranslateHelper()
        {
            FromCulture = englishCulture;
            ToCulture = englishCulture;
            GoogleTranslateUrl = googleDefaultUrlFormat;
        }

        public string TranslateWordOrPhrase(string wordOrPhraseToTranslate)
        {
            var translatedValue = wordOrPhraseToTranslate;

            try
            {
                var url = String.Format(GoogleTranslateUrl, FromCulture, ToCulture, HttpUtility.UrlEncode(wordOrPhraseToTranslate));

                var webClient = new WebClient()
                {
                    Encoding = System.Text.Encoding.Default
                };

                var page = webClient.DownloadString(url);

                translatedValue = GetTranslatedValueFromWebPageData(page, wordOrPhraseToTranslate);
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
