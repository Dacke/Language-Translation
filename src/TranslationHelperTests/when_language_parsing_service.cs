using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using NUnit.Framework;
using Rhino.Mocks;
using TranslationHelper.Engines;
using TranslationHelper.Enums;
using TranslationHelper.Helpers;
using TranslationHelper.Infos;
using TranslationHelper.Services;

namespace TranslationHelperTests
{
    [TestFixture]
    public class when_language_parsing_service_google_english_to_spanish : SpecificationBase
    {
        protected LanguageParsingService sut;
        protected string sourceFilePath;
        protected string targetFilePath;

        private const string SINGLE_RETURN = "TESTONLYASINGLEKEYSHOULDBEFOUND";
        private const string MULTIPLE_RETURN = "TESTMULTIPLEKEYMATCHING";
        private const string VALUE_RETURN = "TESTVALUEFOUNDINTARGETFILE";

        protected List<TranslatedItem> TranslatedItems;

        protected override void Given()
        {
            sourceFilePath = (Environment.CurrentDirectory + "\\SampleResourceFiles\\EnglishSample.resx");
            targetFilePath = String.Format("{0}\\SampleResourceFiles\\target_{1}.resx", Environment.CurrentDirectory, Guid.NewGuid());
            File.Copy((Environment.CurrentDirectory + "\\SampleResourceFiles\\TargetTemplate.resx"), targetFilePath);

            var dispatchService = MockRepository.GenerateMock<IDispatchService>();
            dispatchService.Stub(m => m.Invoke<MessageBoxResult>(null)).IgnoreArguments().Return(MessageBoxResult.Yes);
            dispatchService.Stub(m => m.Invoke<TargetWriteResponse>(null)).IgnoreArguments().Return(TargetWriteResponse.Overwrite);

            var googleEngine = MockRepository.GenerateMock<ITranslateEngine>();
            googleEngine.Stub(m => m.FromCulture).PropertyBehavior().Return("en");
            googleEngine.Stub(m => m.ToCulture).PropertyBehavior().Return("es");
            googleEngine.Stub(m => m.TranslateWordOrPhrase("Value Found In Target File")).Return(VALUE_RETURN);
            googleEngine.Stub(m => m.TranslateWordOrPhrase("Multiple Keys Matching.")).Return(MULTIPLE_RETURN);
            googleEngine.Stub(m => m.TranslateWordOrPhrase("Only a single key should be found.")).Return(SINGLE_RETURN);

            TranslatedItems = new List<TranslatedItem>();

            sut = new LanguageParsingService(dispatchService, googleEngine);
            sut.Translated += (sender, args) => TranslatedItems.Add(args.Item);
        }

        protected override void When()
        {
            sut.ParseFromOnlineSource(sourceFilePath, targetFilePath);
        }

        [Then]
        public void all_items_should_be_translated()
        {
            Assert.That(TranslatedItems.Count, Is.EqualTo(5));
        }

        [Then]
        public void should_match_value_key()
        {
            Assert.That(TranslatedItems.Single(ti => ti.DataKey == "KeyInTarget").Translation, Is.EqualTo(VALUE_RETURN));
        }

        [Then]
        public void should_match_multiple_keys()
        {
            var multiResult = TranslatedItems.Where(ti => ti.DataKey.StartsWith("MultipleMatch"));
            Assert.That(multiResult.Count(), Is.EqualTo(3));
            Assert.That(multiResult.First().Translation, Is.EqualTo(MULTIPLE_RETURN));
        }

        [Then]
        public void should_match_single_value_key()
        {
            Assert.That(TranslatedItems.Single(ti => ti.DataKey == "SingleValue").Translation, Is.EqualTo(SINGLE_RETURN));
        }
    }
}
