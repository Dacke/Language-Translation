namespace TranslationHelper.Engines
{
    public interface ITranslateEngine
    {
        string FromCulture { get; set; }
        string ToCulture { get; set; }
        string TranslateWordOrPhrase(string wordOrPhraseToTranslate);
    }
}