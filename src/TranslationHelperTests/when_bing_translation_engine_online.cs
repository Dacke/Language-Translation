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
    class when_bing_translation_engine_spanish_spec : SpecificationBase
    {
        private BingTranslateEngine sut;

        protected override void Given()
        {
            sut = new BingTranslateEngine() { FromCulture = "en", ToCulture = "es" };
        }

        [Test]
        public void should_translate_single_word()
        {
            Assert.That(sut.TranslateWordOrPhrase("Hold"), Is.EqualTo("Bodega"));
            Assert.That(sut.TranslateWordOrPhrase("Back"), Is.EqualTo("Atrás"));
            Assert.That(sut.TranslateWordOrPhrase("The"), Is.EqualTo("El"));
            Assert.That(sut.TranslateWordOrPhrase("Rain"), Is.EqualTo("Lluvia"));
        }
        
        [Test]
        public void should_translate_multiple_words()
        {
            Assert.That(sut.TranslateWordOrPhrase("Hold back the rain"), Is.EqualTo("Detener la lluvia"));
            Assert.That(sut.TranslateWordOrPhrase("Outside lane"), Is.EqualTo("Fuera de carril"));
            Assert.That(sut.TranslateWordOrPhrase("Fire to blame"), Is.EqualTo("Fuego a quien culpar"));
        }

        [Test]
        public void should_translate_entire_phrase()
        {
            Assert.That(sut.TranslateWordOrPhrase("Yes we're miles away from nowhere and the wind doesn't have a name."),
                        Is.EqualTo("Si estamos lejos de ninguna parte y el viento no tiene nombre."));
            Assert.That(sut.TranslateWordOrPhrase("So call it what you want to call it, it still blows down the lane."),
                        Is.EqualTo("Así lo llaman lo que quieres llamarlo, todavía sopla por el camino."));
        }

        [Test]
        public void should_translate_question_phrase()
        {
            Assert.That(sut.TranslateWordOrPhrase("Won't you please, help me hold back the rain?"),
                        Is.EqualTo("¿No quiere ayudarme por favor, detenga la lluvia?"));
        }
    }

    [TestFixture, Ignore]
    class when_bing_translation_engine_german_spec : SpecificationBase
    {
        private BingTranslateEngine sut;

        protected override void Given()
        {
            sut = new BingTranslateEngine() { FromCulture = "en", ToCulture = "de-DE" };
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
            Assert.That(sut.TranslateWordOrPhrase("Hold back the rain"), Is.EqualTo("Den Regen zurückhalten"));
            Assert.That(sut.TranslateWordOrPhrase("Outside lane"), Is.EqualTo("Außerhalb Lane"));
            Assert.That(sut.TranslateWordOrPhrase("Fire to blame"), Is.EqualTo("Feuer Schuld"));
        }

        [Test]
        public void should_translate_entire_phrase()
        {
            Assert.That(sut.TranslateWordOrPhrase("Yes we're miles away from nowhere and the wind doesn't have a name."),
                        Is.EqualTo("Ja wir sind meilenweit entfernt nirgendwo und der Wind hat einen Namen."));
            Assert.That(sut.TranslateWordOrPhrase("So call it what you want to call it, it still blows down the lane."),
                        Is.EqualTo("So nennen es wie Sie es nennen wollen, es weht immer noch durch die Gasse."));
        }

        [Test]
        public void should_translate_question_phrase()
        {
            Assert.That(sut.TranslateWordOrPhrase("Won't you please, help me hold back the rain?"),
                        Is.EqualTo("Bitte, helfen Sie mir nicht den Regen zurückhalten?"));
        }
    }

    [TestFixture, Ignore]
    class when_bing_translation_engine_simplified_chinese_spec : SpecificationBase
    {
        private BingTranslateEngine sut;

        protected override void Given()
        {
            sut = new BingTranslateEngine() { FromCulture = "en", ToCulture = "zh-CHS" };
        }

        [Test]
        public void should_translate_single_simplified_word()
        {
            Assert.That(sut.TranslateWordOrPhrase("Doctor"), Is.EqualTo("医生"));
            Assert.That(sut.TranslateWordOrPhrase("Who"), Is.EqualTo("谁"));
        }

        [Test]
        public void should_translate_phrases()
        {
            Assert.That(sut.TranslateWordOrPhrase("Don't Blink"), Is.EqualTo("别眨眼"));
            Assert.That(sut.TranslateWordOrPhrase("Blink and your dead"), Is.EqualTo("眨眼间，你死了"));
            Assert.That(sut.TranslateWordOrPhrase("They are fast, faster than you can believe"), Is.EqualTo("它们是快，快，你可以相信"));
        }

        [Test]
        public void should_translate_question()
        {
            Assert.That(sut.TranslateWordOrPhrase("Do you want to build a snowman?"), Is.EqualTo("你想要一个雪人吗？"));
        }
    }
}
