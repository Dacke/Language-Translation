using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using TranslationHelper.Helpers;
using TranslationHelper.Infos;
using Application = Microsoft.Office.Interop.Excel.Application;

namespace TranslationHelper.Engines
{
    public class ExcelTranslateEngine : IDisposable
    {
        private readonly IDispatchService dispatchService;
        private readonly Action<TranslatedItem> translationNotification;
        private Application excelApp;

        private const int OFFSET = 4;
        private const int KEY_COLUMN = 1;
        private const int ENGLISH_COLUMN = 2;
        private const int TRANSLATED_VALUE_COLUMN = 3;

        public ExcelTranslateEngine(IDispatchService dispatchService, Action<TranslatedItem> translationNotification)
        {
            this.dispatchService = dispatchService;
            this.translationNotification = translationNotification;
            excelApp = new Microsoft.Office.Interop.Excel.Application();
        }

        public void TranslateWorkbook(IResourceFileHelper resourceFileHelper, string excelFile, int selectedWorksheet)
        {
            var excelTranslations = GetAllValues(excelFile, selectedWorksheet);
            foreach (var translationResult in excelTranslations)
            {
                var keyValue = translationResult.Key;
                var englishValue = translationResult.EnglishValue;
                var translatedValue = translationResult.Translation;
                if (String.IsNullOrWhiteSpace(translatedValue))
                    continue;

                if (String.IsNullOrWhiteSpace(keyValue) == false)
                {
                    resourceFileHelper.WriteNameValuePairToTarget(keyValue, translatedValue, true);
                    translationNotification(new TranslatedItem { DataKey = keyValue, EnglishValue = englishValue, Translation = translatedValue });
                    continue;
                }

                Dictionary<String, String> sourceValues = resourceFileHelper.GetNameValuesFromSource(englishValue);
                if (sourceValues == null || sourceValues.Any() == false)
                {
                    translationNotification(new TranslatedItem { DataKey = "WARNING", EnglishValue = englishValue, 
                                                                 Translation = "No translation can be made.", Comment = "No Source Key could be found!" });
                    continue;
                }

                if (sourceValues.Count() == 1)
                {
                    var uniqueValue = sourceValues.Single();
                    resourceFileHelper.WriteNameValuePairToTarget(uniqueValue.Key, translatedValue, true);
                    translationNotification(new TranslatedItem { DataKey = uniqueValue.Key, EnglishValue = uniqueValue.Value, Translation = translatedValue });
                    continue;
                }

                if (sourceValues.Count() > 1)
                {
                    var cancelOperation = TranslateMultipleKeyValues(resourceFileHelper, sourceValues, englishValue, translatedValue);
                    if (cancelOperation)
                        break;
                }
            }
        }

        public IEnumerable<ExcelTranslation> GetAllValues(string excelFile, int selectedWorksheet)
        {
            var excelWb = excelApp.Workbooks.Open(excelFile, false, true);
            var workSheet = (Microsoft.Office.Interop.Excel.Worksheet)excelWb.Worksheets[selectedWorksheet];
            var range = workSheet.UsedRange;
            var translationResults = new List<ExcelTranslation>();

            for (int rowIndex = OFFSET; rowIndex <= range.Rows.Count; rowIndex++)
            {
                var keyValue = (range.Cells.Value2[rowIndex, KEY_COLUMN] ?? String.Empty).ToString().Trim();
                var englishValue = (range.Cells.Value2[rowIndex, ENGLISH_COLUMN] ?? String.Empty).ToString().Trim().ToLower();
                var translatedValue = (range.Cells.Value2[rowIndex, TRANSLATED_VALUE_COLUMN] ?? String.Empty).ToString().Trim();

                if (string.IsNullOrWhiteSpace(keyValue) && string.IsNullOrWhiteSpace(englishValue) && string.IsNullOrWhiteSpace(translatedValue))
                    continue;
                
                translationResults.Add(new ExcelTranslation { EnglishValue = englishValue, Key = keyValue, Translation = translatedValue });
            }

            excelWb.Close(false, Type.Missing, Type.Missing);
            excelApp.Workbooks.Close();
            Marshal.ReleaseComObject(range);
            Marshal.ReleaseComObject(excelWb);
            range = null;
            workSheet = null;
            excelWb = null;

            return translationResults;
        }

        public void ExportValuesToWorkbook(IEnumerable<ExcelTranslation> translationValues, string excelFile, int selectedWorksheet)
        {
            var excelWb = excelApp.Workbooks.Open(excelFile, false, false);
            var workSheet = (Microsoft.Office.Interop.Excel.Worksheet)excelWb.Worksheets[selectedWorksheet];
            var rowIndex = OFFSET;

            foreach (var translation in translationValues)
            {
                workSheet.Cells[rowIndex, KEY_COLUMN] = translation.Key;
                workSheet.Cells[rowIndex, ENGLISH_COLUMN] = translation.EnglishValue;
                workSheet.Cells[rowIndex, TRANSLATED_VALUE_COLUMN] = translation.Translation;

                rowIndex++;
            }

            excelWb.Close(true, Type.Missing, Type.Missing);
            excelApp.Workbooks.Close();
            Marshal.ReleaseComObject(excelWb);
            workSheet = null;
            excelWb = null;
        }

        public void Dispose()
        {
            excelApp.Quit();
            Marshal.ReleaseComObject(excelApp);
            Marshal.FinalReleaseComObject(excelApp);
            excelApp = null;
        }

        private bool TranslateMultipleKeyValues(IResourceFileHelper fileHelper, Dictionary<string, string> sourceValues, string englishValue, string translatedValue)
        {
            var cancelOperation = false;

            var writeToAllKeysAnswer = GetWriteToAllKeysAnswer(sourceValues, englishValue, translatedValue);
            switch (writeToAllKeysAnswer)
            {
                case MessageBoxResult.Yes:
                    foreach (var sourceValue in sourceValues)
                        WriteTranslationAndNotify(fileHelper, translatedValue, sourceValue);
                    break;
                case MessageBoxResult.No:
                    var keyResult = MessageBoxResult.No;
                    foreach (var sourceValue in sourceValues)
                        AskOverwriteIndividualKeys(fileHelper, translatedValue, sourceValue);
                    break;
                case MessageBoxResult.Cancel:
                    cancelOperation = true;
                    break;
            }

            return cancelOperation;
        }

        private void AskOverwriteIndividualKeys(IResourceFileHelper fileHelper, string translatedValue, KeyValuePair<string, string> sourceValue)
        {
            MessageBoxResult keyResult;
            KeyValuePair<string, string> value = sourceValue;
            keyResult = dispatchService.Invoke<MessageBoxResult>(new Func<MessageBoxResult>(() =>
                                            MessageBox.Show(String.Format("Use translation \"{0}\" for key \"{1}\"?", translatedValue, value.Key),
                                                            "Use Translation For Key?", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes)));

            if (keyResult == MessageBoxResult.Yes)
                WriteTranslationAndNotify(fileHelper, translatedValue, sourceValue);
        }

        private MessageBoxResult GetWriteToAllKeysAnswer(Dictionary<string, string> sourceValues, string englishValue, string translatedValue)
        {
            var result = dispatchService.Invoke<MessageBoxResult>(new Func<MessageBoxResult>(() =>
                                MessageBox.Show(String.Format("The value \"{0}\" exists for multiple keys.\n\n", englishValue) +
                                                String.Join("\n", sourceValues.Select(v => String.Format("\tKey:{0} => Value:{1}", v.Key, v.Value))) + "\n\n" +
                                                String.Format("Use translation \"{0}\" for all keys?", translatedValue), "Use Translation For All?",
                                                MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.Yes)));

            return result;
        }

        private void WriteTranslationAndNotify(IResourceFileHelper fileHelper, string translatedValue, KeyValuePair<string, string> sourceValue)
        {
            fileHelper.WriteNameValuePairToTarget(sourceValue.Key, translatedValue, true);
            translationNotification(new TranslatedItem { DataKey = sourceValue.Key, EnglishValue = sourceValue.Value, Translation = translatedValue });
        }
    }
}
