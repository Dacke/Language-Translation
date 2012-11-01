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
        private Application excelApp;

        private const int OFFSET = 4;
        private const int KEY_COLUMN = 1;
        private const int ENGLISH_COLUMN = 2;
        private const int TRANSLATED_VALUE_COLUMN = 3;

        public event EventHandler<TranslatedItemEventArgs> ToolOutput = delegate { };

        public ExcelTranslateEngine(IDispatchService dispatchService)
        {
            this.dispatchService = dispatchService;
            excelApp = new Microsoft.Office.Interop.Excel.Application();
        }

        public void TranslateWorkbook(IResourceFileHelper resourceFileHelper, string excelFile, int selectedWorksheet)
        {
            var excelWb = excelApp.Workbooks.Open(excelFile, false, true);
            var workSheet = (Microsoft.Office.Interop.Excel.Worksheet)excelWb.Worksheets[selectedWorksheet];
            var range = workSheet.UsedRange;

            for (int rowIndex = OFFSET; rowIndex <= range.Rows.Count; rowIndex++)
            {
                var keyValue = (range.Cells.Value2[rowIndex, KEY_COLUMN] ?? String.Empty).ToString().Trim();
                var englishValue = (range.Cells.Value2[rowIndex, ENGLISH_COLUMN] ?? String.Empty).ToString().Trim().ToLower();
                var translatedValue = (range.Cells.Value2[rowIndex, TRANSLATED_VALUE_COLUMN] ?? String.Empty).ToString().Trim();
                if (String.IsNullOrWhiteSpace(translatedValue))
                    continue;

                if (String.IsNullOrWhiteSpace(keyValue) == false)
                {
                    resourceFileHelper.WriteNameValuePairToTarget(keyValue, translatedValue, true);
                    //ToolOutput.Invoke(this, new TranslatedItemEventArgs { Output = String.Format("Translated English Key:'{0}' Value:'{1}' => '{2}'", keyValue, englishValue, translatedValue) });
                    continue;
                }

                Dictionary<String, String> sourceValues = resourceFileHelper.GetNameValuesFromSource(englishValue);
                if (sourceValues == null || sourceValues.Any() == false)
                {
                    //ToolOutput.Invoke(this, new TranslatedItemEventArgs { Output = String.Format("WARNING - No Source Key could be found for value:'{0}'!  No translation can be made.", englishValue) });
                    continue;
                }

                if (sourceValues.Count() == 1)
                {
                    var uniqueValue = sourceValues.Single();
                    resourceFileHelper.WriteNameValuePairToTarget(uniqueValue.Key, translatedValue, true);
                    //ToolOutput.Invoke(this, new TranslatedItemEventArgs { Output = String.Format("Translated English Key:'{0}' Value:'{1}' => '{2}'", uniqueValue.Key, uniqueValue.Value, translatedValue) });
                    continue;
                }

                if (sourceValues.Count() > 1)
                {
                    var writeToAllKeysAnswer = dispatchService.Invoke<MessageBoxResult>(new Func<MessageBoxResult>(() => {
                            return MessageBox.Show(String.Format("The value \"{0}\" exists for multiple keys.\n\n", englishValue) +
                                                   String.Join("\n", sourceValues.Select(v => String.Format("\tKey:{0} => Value:{1}", v.Key, v.Value))) + "\n\n" +
                                                   String.Format("Use translation \"{0}\" for all keys?", translatedValue), "Use Translation For All?",
                                                   MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.Yes);
                        }));
                    switch (writeToAllKeysAnswer)
                    {
                        case MessageBoxResult.Yes:
                            foreach (var sourceValue in sourceValues)
                            {
                                resourceFileHelper.WriteNameValuePairToTarget(sourceValue.Key, translatedValue, true);
                                //ToolOutput.Invoke(this, new TranslatedItemEventArgs { Output = String.Format("Translated English Key:'{0}' Value:'{1}' => '{2}'",
                                //                                                                     sourceValue.Key, sourceValue.Value, translatedValue) });
                            }
                            break;
                        case MessageBoxResult.No:
                            var keyResult = MessageBoxResult.No;
                            foreach (var sourceValue in sourceValues)
                            {
                                KeyValuePair<string, string> value = sourceValue;
                                keyResult = dispatchService.Invoke<MessageBoxResult>(new Func<MessageBoxResult>(() => {
                                        return MessageBox.Show(String.Format("Use translation \"{0}\" for key \"{1}\"?", translatedValue, value.Key),
                                                                        "Use Translation For Key?", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes); 
                                    }));

                                if (keyResult == MessageBoxResult.Yes)
                                {
                                    resourceFileHelper.WriteNameValuePairToTarget(sourceValue.Key, translatedValue, true);
                                    //ToolOutput.Invoke(this, new TranslatedItemEventArgs { Output = String.Format("Translated English Key:'{0}' Value:'{1}' => '{2}'",
                                    //                                                                 sourceValue.Key, sourceValue.Value, translatedValue) });
                                }
                            }
                            break;
                        case MessageBoxResult.Cancel:
                            rowIndex = range.Rows.Count;
                            break;
                    }
                }
            }
            
            excelWb.Close(false, Type.Missing, Type.Missing);
            excelApp.Workbooks.Close();
            Marshal.ReleaseComObject(range);
            Marshal.ReleaseComObject(excelWb);
            range = null;
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
    }
}
