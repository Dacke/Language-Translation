namespace TranslationHelper.Infos
{
    public class ExcelTranslation
    {
        public string Key { get; set; }
        public string EnglishValue { get; set; }
        public string Translation { get; set; }

        public override string ToString()
        {
            return string.Format("Key: {0}, English: {1}, Translation: {2}", Key, EnglishValue, Translation);
        }
    }
}
