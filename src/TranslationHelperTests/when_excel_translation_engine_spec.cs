using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using NUnit.Framework;
using Rhino.Mocks;
using TranslationHelper.Engines;
using TranslationHelper.Helpers;
using TranslationHelper.Infos;

namespace TranslationHelperTests
{
    [TestFixture]
    public class when_excel_translation_engine_spec : excel_translation_engine_spec_base
    {
        [Then]
        public void output_should_contain_all_expected_translated_values()
        {
            Assert.That(testOutput.Count(), Is.EqualTo(7));
            Assert.That((testOutput.Count(s => s.Translation == "Activo") > 0), Is.True);
            Assert.That((testOutput.Count(s => s.Translation == "Inactivos") > 0), Is.True);
            Assert.That((testOutput.Count(s => s.Translation == "Tipo de agente") > 0), Is.True);
            Assert.That((testOutput.Count(s => s.Translation == "Terminar") > 0), Is.True);
        }

        [Then]
        public void output_should_include_warning_message_when_missing_source_info()
        {
            Assert.That((testOutput.Count(s => s.DataKey == "WARNING") > 0), Is.True);
        }
    }
    
    [TestFixture]
    public class when_excel_engine_no_overwrite_spec : excel_translation_engine_spec_base
    {
        protected override void Given()
        {
            base.Given();

            var dispatchService = MockRepository.GenerateMock<IDispatchService>();
            dispatchService.Stub(m => m.Invoke<MessageBoxResult>(null)).IgnoreArguments().Return(MessageBoxResult.No);

            sut = new ExcelTranslateEngine(dispatchService);
        }

        [Then]
        public void should_not_translate_all_values()
        {
            Assert.That(testOutput.Count(), Is.EqualTo(4));
        }
    }

    public abstract class excel_translation_engine_spec_base : SpecificationBase
    {
        private string excelFilePath;
        private IResourceFileHelper resourceFileHelper;

        protected ExcelTranslateEngine sut;
        protected List<TranslatedItem> testOutput;

        protected override void Given()
        {
            resourceFileHelper = MockRepository.GenerateMock<IResourceFileHelper>();
            resourceFileHelper.Stub(m => m.GetNameValuesFromSource("active"))
                              .Return(new Dictionary<string, string> { { "Key_Inactive", "Inactive" } });
            resourceFileHelper.Stub(m => m.GetNameValuesFromSource("inactive"))
                              .Return(new Dictionary<string, string> { { "Key_Inactive", "Inactive" } });
            resourceFileHelper.Stub(m => m.GetNameValuesFromSource("agent type"))
                              .Return(new Dictionary<string, string> { { "Key_AgentType", "Agent Type" }, { "Key_Agent_Type", "Agent Type" }, { "Key_TypeOfAgent", "Agent Type" } });

            var dispatchService = MockRepository.GenerateMock<IDispatchService>();
            dispatchService.Stub(m => m.Invoke<MessageBoxResult>(null)).IgnoreArguments().Return(MessageBoxResult.Yes);

            excelFilePath = (Environment.CurrentDirectory + "\\SampleResourceFiles\\ExcelSample.xlsx");
            
            testOutput = new List<TranslatedItem>();

            sut = new ExcelTranslateEngine(dispatchService);
        }

        private void SutOnToolOutput(object sender, TranslatedItemEventArgs outputEventArgs) { testOutput.Add(outputEventArgs.Item); }

        protected override void When()
        {
            sut.ToolOutput += SutOnToolOutput;
            sut.TranslateWorkbook(resourceFileHelper, excelFilePath, 1);
            sut.ToolOutput += SutOnToolOutput;
        }
    }
}
