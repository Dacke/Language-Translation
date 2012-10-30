using System.Net;
using NUnit.Framework;
using Rhino.Mocks;
using TranslationHelper.Engines;

namespace TranslationHelperTests
{
    [TestFixture]
    class when_google_translation_engine_desktop_spec : SpecificationBase
    {
        private GoogleTranslateEngine sut;

        protected override void Given()
        {
            var webClient = new WebClient() {Encoding = System.Text.Encoding.Default};
            sut = new GoogleTranslateEngine(webClient)
                {
                    FromCulture = "en",
                    ToCulture = "es",
                    GoogleTranslateUrl = "http://translate.google.com/?sl={0}&tl={1}&js=n&prev=_t&hl=en&layout=2&eotf=1&text={2}"
                };
        }
    }

    [TestFixture]
    class when_google_translation_engine_default_spec : SpecificationBase
    {
        private GoogleTranslateEngine sut;

        protected override void Given()
        {
            var webClient = MockRepository.GenerateMock<WebClient>();
            webClient.Stub(m => m.DownloadString(""))
                     .Return("");
            
            //sut = new GoogleTranslateEngine(webClient) { FromCulture = "en", ToCulture = "es" };
            sut = new GoogleTranslateEngine() { FromCulture = "en", ToCulture = "es" };
        }

        [Test]
        public void should_translate_single_word()
        {
            Assert.That(sut.TranslateWordOrPhrase("Hold"), Is.EqualTo("Mantener"));
            Assert.That(sut.TranslateWordOrPhrase("Back"), Is.EqualTo("Espalda"));
            Assert.That(sut.TranslateWordOrPhrase("The"), Is.EqualTo("La"));
            Assert.That(sut.TranslateWordOrPhrase("Rain"), Is.EqualTo("Lluvia"));
        }

        [Test]
        public void should_translate_multiple_words()
        {
            Assert.That(sut.TranslateWordOrPhrase("Hold back the rain"), Is.EqualTo("Aguanta la lluvia"));
            Assert.That(sut.TranslateWordOrPhrase("Outside lane"), Is.EqualTo("Fuera de carril"));
            Assert.That(sut.TranslateWordOrPhrase("Fire to blame"), Is.EqualTo("Fuego a culpar"));
        }

        [Test]
        public void should_translate_entire_phrase()
        {
            Assert.That(sut.TranslateWordOrPhrase("Yes we're miles away from nowhere and the wind doesn't have a name."),
                        Is.EqualTo("Sí estamos a millas de distancia de la nada y el viento no tiene un nombre."));
            Assert.That(sut.TranslateWordOrPhrase("So call it what you want to call it still blows down the lane."),
                        Is.EqualTo("Por lo tanto, llámalo como quieras llamarlo todavía sopla por el camino."));
        }

        [Test]
        public void should_translate_question_phrase()
        {
            Assert.That(sut.TranslateWordOrPhrase("Won't you please, help me hold back the rain?"),
                        Is.EqualTo("¿No le gustaría por favor, ayúdame a detener la lluvia?"));
        }
    }
}
