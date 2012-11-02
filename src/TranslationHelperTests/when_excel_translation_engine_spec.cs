using System;
using System.Collections.Generic;
using System.IO;
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

    [TestFixture]
    public class when_excel_engine_export_spec : excel_translation_engine_spec_base
    {
        private List<ExcelTranslation> values;
        private string targetFilePath;

        protected override void Given()
        {
            base.Given();

            var templateFilePath = (Environment.CurrentDirectory + "\\SampleResourceFiles\\TranslationTemplate.xlsx");
            targetFilePath = String.Format("{0}\\SampleResourceFiles\\target_{1}.xlsx", Environment.CurrentDirectory, Guid.NewGuid());
            File.Copy(templateFilePath, targetFilePath, true);

            values = new List<ExcelTranslation>
                {
                    new ExcelTranslation {Key = "Key_Quick", EnglishValue = "Quick", Translation = "Rápido"},
                    new ExcelTranslation {Key = "Key_Brown", EnglishValue = "Brown", Translation ="Marrón"},
                    new ExcelTranslation {Key = "Key_Fox", EnglishValue = "Fox", Translation ="Zorro"},
                    new ExcelTranslation {Key = "Key_Jumped", EnglishValue = "Jumped", Translation ="Saltó"},
                    new ExcelTranslation {Key = "Key_Over", EnglishValue = "Over", Translation ="Por Encima"},
                    new ExcelTranslation {Key = "Key_The", EnglishValue = "The", Translation ="La"},
                    new ExcelTranslation {Key = "Key_Lazy", EnglishValue = "Lazy", Translation ="Perezoso"},
                    new ExcelTranslation {Key = "Key_Dog", EnglishValue = "Dog", Translation ="Perro"},
                };
        }

        protected override void CleanUp()
        {
            base.CleanUp();
            if (File.Exists(targetFilePath))
                File.Delete(targetFilePath);
        }

        protected override void When()
        {
            sut.ExportValuesToWorkbook(values, targetFilePath, WORKSHEET_NUMBER);
        }

        [Then]
        public void should_read_all_written_values()
        {
            var readValues = sut.GetAllValues(targetFilePath, WORKSHEET_NUMBER);
            Assert.That(readValues.Count(), Is.EqualTo(values.Count));
        }
    }

    [TestFixture]
    public class when_excel_engine_read_spec : excel_translation_engine_spec_base
    {
        private IEnumerable<ExcelTranslation> values;

        protected override void When()
        {
            values = sut.GetAllValues(excelFilePath, WORKSHEET_NUMBER);
        }

        [Then]
        public void should_read_all_values_from_spreadsheet()
        {
            Assert.That(values.Count(), Is.EqualTo(5));
        }

        [Then]
        public void should_contain_active_key()
        {
            Assert.That(values.Count(fn => fn.Key == "Key_Active"), Is.EqualTo(1));
        }

        [Then]
        public void should_contain_inactive_english_value()
        {
            Assert.That(values.Count(fn => fn.EnglishValue == "inactive"), Is.EqualTo(1));
        }

        [Then]
        public void should_contain_agent_type_translation()
        {
            Assert.That(values.Count(fn => fn.Translation == "Tipo de agente"), Is.EqualTo(1));
        }
    }

    public abstract class excel_translation_engine_spec_base : SpecificationBase
    {
        private IResourceFileHelper resourceFileHelper;

        protected string excelFilePath;
        protected ExcelTranslateEngine sut;
        protected List<TranslatedItem> testOutput;

        protected const int WORKSHEET_NUMBER = 1;

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
            sut.TranslateWorkbook(resourceFileHelper, excelFilePath, WORKSHEET_NUMBER);
            sut.ToolOutput += SutOnToolOutput;
        }
    }
}
