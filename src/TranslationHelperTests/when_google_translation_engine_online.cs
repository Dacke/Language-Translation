using System.Net;
using NUnit.Framework;
using Rhino.Mocks;
using TranslationHelper.Engines;

namespace TranslationHelperTests
{   
    /// <summary>
    /// This group of tests should only be run on a computer with internet access.
    /// It can be used to make sure that Google has not changed their interface.
    /// </summary>
    /// <remarks>Uses English to Spanish as the translation language</remarks>
    [TestFixture, Ignore]
    class when_google_translation_engine_online_spec : SpecificationBase
    {
        private GoogleTranslateEngine sut;

        protected override void Given()
        {
            sut = new GoogleTranslateEngine() { FromCulture = "en", ToCulture = "es" };
        }

        [Test]
        public void should_translate_single_spanish_word()
        {
            Assert.That(sut.TranslateWordOrPhrase("Hold"), Is.EqualTo("Mantener"));
            Assert.That(sut.TranslateWordOrPhrase("Back"), Is.EqualTo("Espalda"));
            Assert.That(sut.TranslateWordOrPhrase("The"), Is.EqualTo("La"));
            Assert.That(sut.TranslateWordOrPhrase("Rain"), Is.EqualTo("Lluvia"));
        }

        [Test]
        public void should_translate_multiple_spanish_words()
        {
            Assert.That(sut.TranslateWordOrPhrase("Hold back the rain"), Is.EqualTo("Aguanta la lluvia"));
            Assert.That(sut.TranslateWordOrPhrase("Outside lane"), Is.EqualTo("Fuera de carril"));
            Assert.That(sut.TranslateWordOrPhrase("Fire to blame"), Is.EqualTo("Fuego a culpar"));
        }

        [Test]
        public void should_translate_entire_spanish_phrase()
        {
            Assert.That(sut.TranslateWordOrPhrase("Yes we're miles away from nowhere and the wind doesn't have a name."),
                        Is.EqualTo("Sí estamos a millas de distancia de la nada y el viento no tiene un nombre."));
            Assert.That(sut.TranslateWordOrPhrase("So call it what you want to call it still blows down the lane."),
                        Is.EqualTo("Por lo tanto, llámalo como quieras llamarlo todavía sopla por el camino."));
        }

        [Test]
        public void should_translate_question_spanish_phrase()
        {
            Assert.That(sut.TranslateWordOrPhrase("Won't you please, help me hold back the rain?"),
                        Is.EqualTo("¿No le gustaría por favor, ayúdame a detener la lluvia?"));
        }
    }

    [TestFixture, Ignore]
    class when_google_translation_engine_chinese_spec : SpecificationBase
    {
        private GoogleTranslateEngine sut;

        protected override void Given()
        {
            sut = new GoogleTranslateEngine() { FromCulture = "en", ToCulture = "zh-CN" };
        }

        [Test]
        public void should_translate_single_chinese_simplified_word()
        {
            Assert.That(sut.TranslateWordOrPhrase("Doctor"), Is.EqualTo("医生"));
            Assert.That(sut.TranslateWordOrPhrase("Who"), Is.EqualTo("谁"));
        }

        [Test]
        public void should_translate_chinese_phrases()
        {
            Assert.That(sut.TranslateWordOrPhrase("Don't Blink"), Is.EqualTo("千万别眨眼"));
            Assert.That(sut.TranslateWordOrPhrase("Blink and your dead"), Is.EqualTo("闪烁和你死了"));
            Assert.That(sut.TranslateWordOrPhrase("They are fast, faster than you can believe"), Is.EqualTo("他们的速度快，速度比你可以相信"));
        }
        
    }
}
