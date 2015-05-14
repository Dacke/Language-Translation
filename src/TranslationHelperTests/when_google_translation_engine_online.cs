using NUnit.Framework;
using TranslationHelper.Engines;

namespace TranslationHelperTests
{   
    /// <summary>
    /// This group of tests should only be run on a computer with internet access.
    /// It can be used to make sure that Google has not changed their interface.
    /// </summary>
    /// <remarks>Uses English to Spanish as the translation language</remarks>
    [TestFixture, Ignore]
    class when_google_translation_engine_spanish_spec : SpecificationBase
    {
        private GoogleTranslateEngine sut;

        protected override void Given()
        {
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
            Assert.That(sut.TranslateWordOrPhrase("Hold back the rain"), Is.EqualTo("Mantenga la espalda de la lluvia"));
            Assert.That(sut.TranslateWordOrPhrase("Outside lane"), Is.EqualTo("Carril exterior"));
            Assert.That(sut.TranslateWordOrPhrase("Fire to blame"), Is.EqualTo("Fuego culpa"));
        }

        [Test]
        public void should_translate_entire_phrase()
        {
            Assert.That(sut.TranslateWordOrPhrase("Yes we're miles away from nowhere and the wind doesn't have a name."),
                        Is.EqualTo("Sí estamos millas de distancia de la nada y el viento no tiene un nombre."));
            Assert.That(sut.TranslateWordOrPhrase("So call it what you want to call it still blows down the lane."),
                        Is.EqualTo("Así llámalo como quieras llamarlo todavía sopla por el carril."));
        }

        [Test]
        public void should_translate_question_phrase()
        {
            Assert.That(sut.TranslateWordOrPhrase("Won't you please, help me hold back the rain?"),
                        Is.EqualTo("¿No le gustaría por favor, que me ayude a contener la lluvia?"));
        }
    }

    [TestFixture, Ignore]
    class when_google_translation_engine_german_spec : SpecificationBase
    {
        private GoogleTranslateEngine sut;

        protected override void Given()
        {
            sut = new GoogleTranslateEngine() { FromCulture = "en", ToCulture = "de-DE" };
        }

        [Test]
        public void should_translate_single_word()
        {
            Assert.That(sut.TranslateWordOrPhrase("Hold"), Is.EqualTo("Halten"));
            Assert.That(sut.TranslateWordOrPhrase("Back"), Is.EqualTo("Zurück"));
            Assert.That(sut.TranslateWordOrPhrase("The"), Is.EqualTo("Die"));
            Assert.That(sut.TranslateWordOrPhrase("Rain"), Is.EqualTo("Regen"));
        }

        [Test]
        public void should_translate_multiple_words()
        {
            Assert.That(sut.TranslateWordOrPhrase("Hold back the rain"), Is.EqualTo("Zurückhalten, die regen"));
            Assert.That(sut.TranslateWordOrPhrase("Outside lane"), Is.EqualTo("Überholspur"));
            Assert.That(sut.TranslateWordOrPhrase("Fire to blame"), Is.EqualTo("Feuer schuld"));
        }

        [Test]
        public void should_translate_entire_phrase()
        {
            Assert.That(sut.TranslateWordOrPhrase("Yes we're miles away from nowhere and the wind doesn't have a name."),
                        Is.EqualTo("Ja, wir sind Meilen entfernt von dem Nichts und der Wind nicht einen Namen haben."));
            Assert.That(sut.TranslateWordOrPhrase("So call it what you want to call it, it still blows down the lane."),
                        Is.EqualTo("So nennen Sie es wie Sie es nennen wollen, ist es immer noch weht durch die Gasse."));
        }

        [Test]
        public void should_translate_question_phrase()
        {
            Assert.That(sut.TranslateWordOrPhrase("Won't you please, help me hold back the rain?"),
                        Is.EqualTo("Werden Sie bitte nicht, mir helfen, die regen zurückhalten?"));
        }
    }

    [TestFixture, Ignore]
    class when_google_translation_engine_simplified_chinese_spec : SpecificationBase
    {
        private GoogleTranslateEngine sut;

        protected override void Given()
        {
            sut = new GoogleTranslateEngine() { FromCulture = "en", ToCulture = "zh-CN" };
        }

        [Test]
        public void should_translate_single_word()
        {
            Assert.That(sut.TranslateWordOrPhrase("Doctor"), Is.EqualTo("医生"));
            Assert.That(sut.TranslateWordOrPhrase("Who"), Is.EqualTo("谁"));
        }

        [Test]
        public void should_translate_phrases()
        {
            Assert.That(sut.TranslateWordOrPhrase("Don't Blink"), Is.EqualTo("千万别眨眼"));
            Assert.That(sut.TranslateWordOrPhrase("Blink and your dead"), Is.EqualTo("闪烁和你死了"));
            Assert.That(sut.TranslateWordOrPhrase("They are fast, faster than you can believe"), Is.EqualTo("他们的速度快，速度比你可以相信"));
        }
        
    }
}
