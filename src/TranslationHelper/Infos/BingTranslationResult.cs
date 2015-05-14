namespace TranslationHelper.Infos
{
    /*
    [
        {
            "Alignment":"0:10-0:9",
            "From":"en",
            "OriginalTextSentenceLengths":[11],
            "TranslatedText":"Hola mundo",
            "TranslatedTextSentenceLengths":[10]
        }
    ]
    */

    internal class BingTranslationResult
    {
        public string Alignment { get; set; }
        public string From { get; set; }
        public int[] OriginalTextSentenceLengths { get; set; }
        public string TranslatedText { get; set; }
        public int[] TranslatedTextSentenceLengths { get; set; }
    }
}
