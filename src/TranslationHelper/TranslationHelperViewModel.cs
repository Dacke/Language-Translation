using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TranslationHelper.Engines;
using TranslationHelper.Helpers;
using TranslationHelper.Infos;
using TranslationHelper.Services;

namespace TranslationHelper
{
    public class TranslationHelperViewModel : INotifyPropertyChanged
    {
        #region Fields

        private string sourceFile;
        private string targetFile;
        private string translationFile;
        private bool useGoogleTranslationEngine;
        private bool translationFileEnabled;
        private LanguageCode selectedLanguageCode;
        private ObservableCollection<TranslatedItem> translatedItems;

        #endregion

        #region Headers

        public string BrowseLabel { get { return "Browse"; } }
        public string EnglishResourceFileLabel { get { return "English String Resource File"; } }
        public string OutputLabel { get { return "Translation Output"; } }
        public string TargetResourceFileLabel { get { return "Target Resource File"; } }
        public string TranslateLabel { get { return "Translate"; } }
        public string TranslationsFileLabel { get { return "Translations File"; } }
        public string UseGoogleLabel { get { return "Check to use Google for the translation engine"; } }
        public string ExportLabel { get { return "Export"; } }

        #endregion

        public string SourceFile
        {
            get { return sourceFile; }
            set { 
                sourceFile = value;
                OnPropertyChanged(p => p.SourceFile);
            }
        }
        public string TargetFile
        {
            get { return targetFile; }
            set
            {
                targetFile = value;
                OnPropertyChanged(p => p.TargetFile);
            }
        }
        public string TranslationFile
        {
            get { return translationFile; }
            set
            {
                translationFile = value;
                OnPropertyChanged(p => p.TranslationFile);
            }
        }
        public bool TranslationFileEnabled
        {
            get { return translationFileEnabled; }
            set
            {
                translationFileEnabled = value;
                OnPropertyChanged(p => p.TranslationFileEnabled);
            }
        }
        public bool UseGoogleTranslationEngine
        {
            get { return useGoogleTranslationEngine; }
            set
            {
                useGoogleTranslationEngine = value;
                OnPropertyChanged(p => p.UseGoogleTranslationEngine);
            }
        }
        public LanguageCode SelectedLanguageCode
        {
            get { return selectedLanguageCode; }
            set
            {
                selectedLanguageCode = value;
                OnPropertyChanged(p => p.SelectedLanguageCode);
            }
        }
        public ObservableCollection<TranslatedItem> TranslatedItems
        {
            get { return translatedItems; }
            set
            {
                translatedItems = value;
                if (View != null) 
                    View.ScrollOutput();
            }
        }
        public ObservableCollection<LanguageCode> LanguageCodes { get; private set; }
        public ITranslationHelperView View { get; set; }
        public ICommand BrowseSourceFileCommand { get; set; }
        public ICommand BrowseTargetFileCommand { get; set; }
        public ICommand BrowseTranslationFileCommand { get; set; }
        public ICommand TranslateFromGoogleCommand { get; set; }
        public ICommand TranslateCommand { get; set; }
        public ICommand ExportCommand { get; set; }
        
        public event PropertyChangedEventHandler PropertyChanged = delegate { };
        
        public TranslationHelperViewModel(ITranslationHelperView view)
        {
            TranslatedItems = new ObservableCollection<TranslatedItem>();
            LanguageCodes = FillLanguageCodes();
            UseGoogleTranslationEngine = true;
            SelectedLanguageCode = LanguageCodes.Single(lc => lc.Code == "es");

            BrowseSourceFileCommand = new DelegateCommand(m => SourceBrowseClicked());
            BrowseTargetFileCommand = new DelegateCommand(m => TargetBrowseClicked());
            BrowseTranslationFileCommand = new DelegateCommand(m => TranslationFileBrowseClicked());
            TranslateFromGoogleCommand = new DelegateCommand(m => GoogleTranslationClicked());
            TranslateCommand = new DelegateCommand(m => TranslateButtonClicked());
            ExportCommand = new DelegateCommand(m => ExportButtonClicked());

            GoogleTranslationClicked();

            TranslatedItems.CollectionChanged += (sender, args) => { if (View != null) View.ScrollOutput(); };

            //  DEBUGGING VALUES - COMMENT OUT DURING PRODUCTION
            //var desktopFolder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            //SourceFile = String.Format(@"{0}\testfiles\EnglishResourceTestFile.resx", desktopFolder);
            //TargetFile = String.Format(@"{0}\testfiles\empty.resx", desktopFolder);
            //TranslationFile = String.Format(@"{0}\testfiles\ExcelSample.xlsx", desktopFolder);
            //  DEBUGGING VALUES - COMMENT OUT DURING PRODUCTION
            
            View = view;
            View.SetModel(this);
        }
        
        private void OnPropertyChanged(Expression<Func<TranslationHelperViewModel, object>> propertyExpression)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyExpression.GetPropertyName()));
        }

        private void SourceBrowseClicked()
        {
            const string dialogTitle = "Select English File";
            const string fileFilter = "Resource Files(*.resx)|*.resx|All Files(*.*)|*.*";

            View.OpenBrowseFileDialog(dialogTitle, fileFilter, p => p.SourceFile);
        }

        private void TargetBrowseClicked()
        {
            const string dialogTitle = "Select Target File";
            const string fileFilter = "Resource Files(*.resx)|*.resx|All Files(*.*)|*.*";

            View.OpenBrowseFileDialog(dialogTitle, fileFilter, p => p.TargetFile);
        }

        private void TranslationFileBrowseClicked()
        {
            const string dialogTitle = "Select Translation File";
            const string fileFilter = "Excel SpreadSheet|*.xls;*.xlsx|All Files(*.*)|*.*";

            View.OpenBrowseFileDialog(dialogTitle, fileFilter, p => p.TranslationFile);
        }

        private void GoogleTranslationClicked()
        {
            TranslationFileEnabled = (!UseGoogleTranslationEngine);
        }

        private void TranslateButtonClicked()
        {
            try
            {
                ValidateArguments();

                TranslatedItems.Clear();
                Task.Factory.StartNew(PerformTranslation, TaskCreationOptions.AttachedToParent);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
                if (Debugger.IsAttached)
                    Debugger.Break();
                MessageBox.Show(ex.Message, ex.Source, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PerformTranslation()
        {
            var dispatchService = new DispatchService();
            var googleEngine = new GoogleTranslateEngine { ToCulture = SelectedLanguageCode.Code };
            dispatchService.Invoke(() => ((Window) View).Cursor = Cursors.Wait);
            dispatchService.Invoke(() => TranslatedItems.Add(new TranslatedItem {Comment = "Translation Started"}));
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            using (var langParser = new LanguageParsingService(dispatchService, googleEngine))
            {
                langParser.Translated += LangParserItemTranslated;

                if (UseGoogleTranslationEngine)
                    langParser.ParseFromGoogle(SourceFile, TargetFile);
                else
                    langParser.ParseFromExcel(SourceFile, TargetFile, TranslationFile);

                langParser.Translated -= LangParserItemTranslated;
            }

            stopWatch.Stop();
            dispatchService.Invoke(() => TranslatedItems.Add(new TranslatedItem
                {Comment = String.Format("Translation Completed.  ({0} seconds elapsed)", stopWatch.Elapsed.TotalSeconds)}));
            dispatchService.Invoke(() => ((Window) View).Cursor = Cursors.Arrow);

            MessageBox.Show("The translation is complete.  Please check the output window for a list of items that have been translated.",
                            "Done", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        
        private void ExportButtonClicked()
        {
            try
            {
                var dispatchService = new DispatchService();
                

                dispatchService.Invoke(() => ((Window)View).Cursor = Cursors.Wait);

                var exportFilename = (Environment.CurrentDirectory + "\\TranslationExport_" +
                                      Guid.NewGuid().ToString().Trim(new char[] {'{', '}'}).Substring(0, 5) + ".xlsx");
                
                File.Copy((Environment.CurrentDirectory + "\\TranslationTemplate.xlsx"), exportFilename, true);

                var exportingValues = ExcelTranslations();

                var excelEngine = new ExcelTranslateEngine(dispatchService, t => LangParserItemTranslated(this, new TranslatedItemEventArgs { Item = t }));
                excelEngine.ExportValuesToWorkbook(exportingValues, exportFilename, 1);

                dispatchService.Invoke(() => ((Window)View).Cursor = Cursors.Arrow);

                Process.Start(exportFilename);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
                if (Debugger.IsAttached)
                    Debugger.Break();
                MessageBox.Show(ex.Message, ex.Source, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private IEnumerable<ExcelTranslation> ExcelTranslations()
        {
            IEnumerable<ExcelTranslation> exportingValues;
            using (var resourceFileHelper = new ResourceFileHelper(SourceFile, TargetFile))
            {
                var sourceInformation = resourceFileHelper.GetAllNameValuesFromSource();
                var targetInformation = resourceFileHelper.GetAllNameValuesFromTarget();

                exportingValues = from srcInfo in sourceInformation
                                  join trgInfo in targetInformation on srcInfo.Key equals trgInfo.Key
                                  select new ExcelTranslation
                                      {
                                          Key = trgInfo.Key,
                                          EnglishValue = srcInfo.Value,
                                          Translation = trgInfo.Value
                                      };
            }
            return exportingValues;
        }

        private void ValidateArguments()
        {
            if (String.IsNullOrWhiteSpace(SourceFile) || SourceFile.EndsWith("resx") == false)
                throw new Exception("You have not filled in a value for the English String Resource File. (*.resx)");

            if (String.IsNullOrWhiteSpace(TargetFile) || TargetFile.EndsWith("resx") == false)
                throw new Exception("You have not filled in a value for the Target Resource File. (*.resx)");

            if (UseGoogleTranslationEngine != true)
            {
                if (String.IsNullOrWhiteSpace(TranslationFile) ||
                    (TranslationFile.EndsWith("xls") == false & TranslationFile.EndsWith("xlsx") == false))
                    throw new Exception("You have not filled in a value for the Translations File. (*.xls, *.xlsx)");
            }
        }

        private static ObservableCollection<LanguageCode> FillLanguageCodes()
        {
            var result = new ObservableCollection<LanguageCode>
                {
                    new LanguageCode {Code = "af", Name = "Afrikaans"},
                    new LanguageCode {Code = "sq", Name = "Albanian"},
                    new LanguageCode {Code = "ar", Name = "Arabic"},
                    new LanguageCode {Code = "hy", Name = "Armenian"},
                    new LanguageCode {Code = "az", Name = "Azerbaijani"},
                    new LanguageCode {Code = "eu", Name = "Basque"},
                    new LanguageCode {Code = "be", Name = "Belarusian"},
                    new LanguageCode {Code = "bn", Name = "Bengali"},
                    new LanguageCode {Code = "bg", Name = "Bulgarian"},
                    new LanguageCode {Code = "ca", Name = "Catalan"},
                    new LanguageCode {Code = "zh-CN", Name = "Chinese (Simplified)"},
                    new LanguageCode {Code = "zh-TW", Name = "Chinese (Traditional)"},
                    new LanguageCode {Code = "hr", Name = "Croatian"},
                    new LanguageCode {Code = "cs", Name = "Czech"},
                    new LanguageCode {Code = "da", Name = "Danish"},
                    new LanguageCode {Code = "nl", Name = "Dutch"},
                    new LanguageCode {Code = "en", Name = "English"},
                    new LanguageCode {Code = "eo", Name = "Esperanto"},
                    new LanguageCode {Code = "et", Name = "Estonian"},
                    new LanguageCode {Code = "tl", Name = "Filipino"},
                    new LanguageCode {Code = "fi", Name = "Finnish"},
                    new LanguageCode {Code = "fr", Name = "French"},
                    new LanguageCode {Code = "gl", Name = "Galician"},
                    new LanguageCode {Code = "ka", Name = "Georgian"},
                    new LanguageCode {Code = "de", Name = "German"},
                    new LanguageCode {Code = "el", Name = "Greek"},
                    new LanguageCode {Code = "gu", Name = "Gujarati"},
                    new LanguageCode {Code = "ht", Name = "Haitian Creole"},
                    new LanguageCode {Code = "iw", Name = "Hebrew"},
                    new LanguageCode {Code = "hi", Name = "Hindi"},
                    new LanguageCode {Code = "hu", Name = "Hungarian"},
                    new LanguageCode {Code = "is", Name = "Icelandic"},
                    new LanguageCode {Code = "id", Name = "Indonesian"},
                    new LanguageCode {Code = "ga", Name = "Irish"},
                    new LanguageCode {Code = "it", Name = "Italian"},
                    new LanguageCode {Code = "ja", Name = "Japanese"},
                    new LanguageCode {Code = "kn", Name = "Kannada"},
                    new LanguageCode {Code = "ko", Name = "Korean"},
                    new LanguageCode {Code = "lo", Name = "Lao"},
                    new LanguageCode {Code = "la", Name = "Latin"},
                    new LanguageCode {Code = "lv", Name = "Latvian"},
                    new LanguageCode {Code = "lt", Name = "Lithuanian"},
                    new LanguageCode {Code = "mk", Name = "Macedonian"},
                    new LanguageCode {Code = "ms", Name = "Malay"},
                    new LanguageCode {Code = "mt", Name = "Maltese"},
                    new LanguageCode {Code = "no", Name = "Norwegian"},
                    new LanguageCode {Code = "fa", Name = "Persian"},
                    new LanguageCode {Code = "pl", Name = "Polish"},
                    new LanguageCode {Code = "pt", Name = "Portuguese"},
                    new LanguageCode {Code = "ro", Name = "Romanian"},
                    new LanguageCode {Code = "ru", Name = "Russian"},
                    new LanguageCode {Code = "sr", Name = "Serbian"},
                    new LanguageCode {Code = "sk", Name = "Slovak"},
                    new LanguageCode {Code = "sl", Name = "Slovenian"},
                    new LanguageCode {Code = "es", Name = "Spanish"},
                    new LanguageCode {Code = "sw", Name = "Swahili"},
                    new LanguageCode {Code = "sv", Name = "Swedish"},
                    new LanguageCode {Code = "ta", Name = "Tamil"},
                    new LanguageCode {Code = "te", Name = "Telugu"},
                    new LanguageCode {Code = "th", Name = "Thai"},
                    new LanguageCode {Code = "tr", Name = "Turkish"},
                    new LanguageCode {Code = "uk", Name = "Ukrainian"},
                    new LanguageCode {Code = "ur", Name = "Urdu"},
                    new LanguageCode {Code = "vi", Name = "Vietnamese"},
                    new LanguageCode {Code = "cy", Name = "Welsh"},
                    new LanguageCode {Code = "yi", Name = "Yiddish"}
                };

            return result;
        }

        private void LangParserItemTranslated(object sender, TranslatedItemEventArgs translatedItemEventArgs)
        {
            View.Dispatcher.BeginInvoke(new Action(() => TranslatedItems.Add(translatedItemEventArgs.Item)));
        }
    }
}
