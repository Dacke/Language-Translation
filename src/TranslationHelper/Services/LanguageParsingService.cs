using System;
using System.Windows;
using TranslationHelper.Engines;
using TranslationHelper.Enums;
using TranslationHelper.Helpers;
using TranslationHelper.Infos;

namespace TranslationHelper.Services
{
    public class LanguageParsingService : IDisposable
    {
        private readonly IDispatchService dispatchService;
        private readonly ITranslateEngine onlineTranslationEngine;

        public event EventHandler<TranslatedItemEventArgs> Translated = delegate { };
        
        public LanguageParsingService(IDispatchService dispatchService, ITranslateEngine onlineTranslationEngine)
        {
            this.dispatchService = dispatchService;
            this.onlineTranslationEngine = onlineTranslationEngine;
        }

        public void Dispose() { }

        public void ParseFromOnlineSource(string sourceFile, string targetFile)
        {
            var writeTargetResult = TargetWriteResponse.Skip;

            using (var resourceFileHelper = new ResourceFileHelper(sourceFile, targetFile))
            {
                foreach (var sourcePair in resourceFileHelper.GetAllNameValuesFromSource())
                {
                    var translatedValue = onlineTranslationEngine.TranslateWordOrPhrase(sourcePair.Value);
                    var existingTargetValue = resourceFileHelper.GetValueFromTargetUsingKey(sourcePair.Key);

                    if (writeTargetResult != TargetWriteResponse.OverwriteAll)
                    {
                        writeTargetResult = OverwriteWarningWithResult(existingTargetValue, translatedValue);
                        if (writeTargetResult == TargetWriteResponse.Cancel)
                        {
                            dispatchService.Invoke(new Action(() => MessageBox.Show("The translation operation has been aborted.", "Aborted",
                                                                                    MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK)));
                            break;
                        }
                    }

                    if (writeTargetResult == TargetWriteResponse.Skip) continue;

                    resourceFileHelper.WriteNameValuePairToTarget(sourcePair.Key, translatedValue, true);

                    Translated.Invoke(this, new TranslatedItemEventArgs
                        {
                            Item = new TranslatedItem { DataKey = sourcePair.Key, EnglishValue = sourcePair.Value, Translation = translatedValue }
                        });
                }
            }
        }

        public void ParseFromExcel(string sourceFile, string targetFile, string translationFile)
        {
            var excelEngine = new ExcelTranslateEngine(dispatchService, t => Translated.Invoke(this, new TranslatedItemEventArgs() { Item = t }));
            
            using (var resourceFileHelper = new ResourceFileHelper(sourceFile, targetFile))
            {
                excelEngine.TranslateWorkbook(resourceFileHelper, translationFile, 1);
            }
        }
        
        private TargetWriteResponse OverwriteWarningWithResult(string existingTargetValue, string translatedValue)
        {
            if (string.IsNullOrWhiteSpace(existingTargetValue)) return TargetWriteResponse.Overwrite;
            if (existingTargetValue.Equals(translatedValue, StringComparison.InvariantCultureIgnoreCase)) return TargetWriteResponse.Skip;

            var response = dispatchService.Invoke<TargetWriteResponse>(
                    new Func<TargetWriteResponse>(() => AskUserOverwriteResponse(existingTargetValue, translatedValue)));

            return response;
        }

        private TargetWriteResponse AskUserOverwriteResponse(string existingTargetValue, string translatedValue)
        {
            var response = TargetWriteResponse.Skip;

            var vm = new OverwriteWarningViewModel(new OverwriteWarning())
            {
                Question = "Do you wish to overwrite the existing value with the newly translated one?",
                Description = "A value already exists in the targeted resource file.",
                ExistingValue = existingTargetValue,
                TranslationValue = translatedValue,
            };
            vm.View.ShowDialog();
            switch (vm.Answer)
            {
                case OverwriteResult.Yes:
                    response = TargetWriteResponse.Overwrite;
                    break;
                case OverwriteResult.YesToAll:
                    response = TargetWriteResponse.OverwriteAll;
                    break;
                case OverwriteResult.Cancel:
                    response = TargetWriteResponse.Cancel;
                    break;
            }
            return response;
        }
    }
}
