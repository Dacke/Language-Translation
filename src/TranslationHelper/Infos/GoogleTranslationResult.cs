namespace TranslationHelper.Infos
{
    internal class GoogleTranslationResult
    {
        public GoogleTranslation[] sentences { get; set; }
        public GoogleDictionaryEntry[] dict { get; set; }
        public string src { get; set; }
        public int server_time { get; set; }
    }
    
    internal class GoogleTranslation
    {
        public string trans { get; set; }
        public string orig { get; set; }
        public string translit { get; set; }
        public string src_translit { get; set; }
    }

    internal class GoogleDictionaryEntry
    {
        public string pos { get; set; }
        public string[] terms { get; set; }
        public TermInfo[] entry { get; set; }

    }

    internal class TermInfo
    {
        public string word { get; set; }
        public string[] reverse_translation { get; set; }
        public double score { get; set; }
    }
}
