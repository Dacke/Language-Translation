using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using TranslationHelper.Engines;

namespace TranslationHelper
{
    public class TranslationHelperViewModel : INotifyPropertyChanged
    {
        #region Fields

        private const string TRANSLATION_TO_SKIP = "(please inactivate)";

        private string sourceFile;
        private string targetFile;
        private string translationFile;
        private bool useGoogleTranslationEngine;
        private bool translationFileEnabled;
        private LanguageCode selectedLanguageCode;
        private ObservableCollection<string> translatedItems;
        #endregion

        #region Properties

        #region Headers

        public string BrowseLabel { get { return "Browse"; } }
        public string EnglishResourceFileLabel { get { return "English String Resource File"; } }
        public string OutputLabel { get { return "Translation Output"; } }
        public string TargetResourceFileLabel { get { return "Target Resource File"; } }
        public string TranslateLabel { get { return "Translate"; } }
        public string TranslationsFileLabel { get { return "Translations File"; } }
        public string UseGoogleLabel { get { return "Check to use Google for the translation engine"; } }

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
        public ObservableCollection<string> TranslatedItems
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

        #endregion

        #region Events

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        #endregion

        #region Public Methods

        public TranslationHelperViewModel(ITranslationHelperView view)
        {
            this.TranslatedItems = new ObservableCollection<string>();
            this.LanguageCodes = FillLanguageCodes();
            this.UseGoogleTranslationEngine = true;
            this.SelectedLanguageCode = LanguageCodes.Single(lc => lc.Code == "es");

            BrowseSourceFileCommand = new DelegateCommand(m => this.OnSourceBrowse());
            BrowseTargetFileCommand = new DelegateCommand(m => this.OnTargetBrowse());
            BrowseTranslationFileCommand = new DelegateCommand(m => this.OnTranslationFileBrowse());
            TranslateFromGoogleCommand = new DelegateCommand(m => this.OnGoogleTranslationClick());
            TranslateCommand = new DelegateCommand(m => this.OnTranslateCommand());

            OnGoogleTranslationClick();

            this.TranslatedItems.CollectionChanged += (sender, args) => { if (View != null) View.ScrollOutput(); };

            //  DEBUGGING VALUES - COMMENT OUT DURING PRODUCTION
            this.SourceFile = @"C:\Users\eDorothy\Desktop\testfiles\EnglishResourceTestFile.resx";
            this.TargetFile = @"C:\Users\eDorothy\Desktop\testfiles\empty.resx";
            //  DEBUGGING VALUES - COMMENT OUT DURING PRODUCTION
            
            this.View = view;
            View.SetModel(this);
        }

        #endregion

        #region Private Methods
        
        private void OnPropertyChanged(Expression<Func<TranslationHelperViewModel, object>> propertyExpression)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyExpression.GetPropertyName()));
        }

        private void OnSourceBrowse()
        {
            const string dialogTitle = "Select English File";
            const string fileFilter = "Resource Files(*.resx)|*.resx|All Files(*.*)|*.*";

            View.OpenBrowseFileDialog(dialogTitle, fileFilter, p => p.SourceFile);
        }

        private void OnTargetBrowse()
        {
            const string dialogTitle = "Select Target File";
            const string fileFilter = "Resource Files(*.resx)|*.resx|All Files(*.*)|*.*";

            View.OpenBrowseFileDialog(dialogTitle, fileFilter, p => p.TargetFile);
        }

        private void OnTranslationFileBrowse()
        {
            const string dialogTitle = "Select Translation File";
            const string fileFilter = "Excel SpreadSheet|*.xls;*.xlsx|All Files(*.*)|*.*";

            View.OpenBrowseFileDialog(dialogTitle, fileFilter, p => p.TranslationFile);
        }

        private void OnGoogleTranslationClick()
        {
            TranslationFileEnabled = (!UseGoogleTranslationEngine);
        }

        private void OnTranslateCommand()
        {
            try
            {
                ValidateArguments();

                TranslatedItems.Clear();

                Task.Factory.StartNew(() =>
                    {
                        DispatchService.Invoke(() => ((Window)View).Cursor = Cursors.Wait);
                        DispatchService.Invoke(() => TranslatedItems.Add("Translation Started"));
                        var stopWatch = new Stopwatch();
                        stopWatch.Start();

                        if (UseGoogleTranslationEngine)
                            this.ParseFromGoogle();
                        else
                            this.ParseTranslationFile();

                        stopWatch.Stop();
                        DispatchService.Invoke(() => TranslatedItems.Add(String.Format("Translation Completed.  ({0} seconds elapsed)", stopWatch.Elapsed.TotalSeconds))); 
                        DispatchService.Invoke(() => ((Window)View).Cursor = Cursors.Arrow);
                        
                        MessageBox.Show("The translation is complete.  Please check the output window for a list of items that have been translated.", "Done",
                                     MessageBoxButton.OK, MessageBoxImage.Information);
                    }, TaskCreationOptions.AttachedToParent);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
                if (Debugger.IsAttached)
                    Debugger.Break();
                MessageBox.Show(ex.Message, ex.Source, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ValidateArguments()
        {
            if (String.IsNullOrWhiteSpace(this.SourceFile) || this.SourceFile.EndsWith("resx") == false)
                throw new Exception("You have not filled in a value for the English String Resource File. (*.resx)");

            if (String.IsNullOrWhiteSpace(this.TargetFile) || this.TargetFile.EndsWith("resx") == false)
                throw new Exception("You have not filled in a value for the Target Resource File. (*.resx)");

            if (this.UseGoogleTranslationEngine != true)
            {
                if (String.IsNullOrWhiteSpace(this.TranslationFile) ||
                    (this.TranslationFile.EndsWith("xls") == false & this.TranslationFile.EndsWith("xlsx") == false))
                    throw new Exception("You have not filled in a value for the Translations File. (*.xls, *.xlsx)");
            }
        }

        private ObservableCollection<LanguageCode> FillLanguageCodes()
        {
            var result = new ObservableCollection<LanguageCode>();

            result.Add(new LanguageCode() { Code = "af", Name = "Afrikaans" });
            result.Add(new LanguageCode() { Code = "sq", Name = "Albanian" });
            result.Add(new LanguageCode() { Code = "ar", Name = "Arabic" });
            result.Add(new LanguageCode() { Code = "hy", Name = "Armenian" });
            result.Add(new LanguageCode() { Code = "az", Name = "Azerbaijani" });
            result.Add(new LanguageCode() { Code = "eu", Name = "Basque" });
            result.Add(new LanguageCode() { Code = "be", Name = "Belarusian" });
            result.Add(new LanguageCode() { Code = "bn", Name = "Bengali" });
            result.Add(new LanguageCode() { Code = "bg", Name = "Bulgarian" });
            result.Add(new LanguageCode() { Code = "ca", Name = "Catalan" });
            result.Add(new LanguageCode() { Code = "zh-CN", Name = "Chinese (Simplified)" });
            result.Add(new LanguageCode() { Code = "zh-TW", Name = "Chinese (Traditional)" });
            result.Add(new LanguageCode() { Code = "hr", Name = "Croatian" });
            result.Add(new LanguageCode() { Code = "cs", Name = "Czech" });
            result.Add(new LanguageCode() { Code = "da", Name = "Danish" });
            result.Add(new LanguageCode() { Code = "nl", Name = "Dutch" });
            result.Add(new LanguageCode() { Code = "en", Name = "English" });
            result.Add(new LanguageCode() { Code = "eo", Name = "Esperanto" });
            result.Add(new LanguageCode() { Code = "et", Name = "Estonian" });
            result.Add(new LanguageCode() { Code = "tl", Name = "Filipino" });
            result.Add(new LanguageCode() { Code = "fi", Name = "Finnish" });
            result.Add(new LanguageCode() { Code = "fr", Name = "French" });
            result.Add(new LanguageCode() { Code = "gl", Name = "Galician" });
            result.Add(new LanguageCode() { Code = "ka", Name = "Georgian" });
            result.Add(new LanguageCode() { Code = "de", Name = "German" });
            result.Add(new LanguageCode() { Code = "el", Name = "Greek" });
            result.Add(new LanguageCode() { Code = "gu", Name = "Gujarati" });
            result.Add(new LanguageCode() { Code = "ht", Name = "Haitian Creole" });
            result.Add(new LanguageCode() { Code = "iw", Name = "Hebrew" });
            result.Add(new LanguageCode() { Code = "hi", Name = "Hindi" });
            result.Add(new LanguageCode() { Code = "hu", Name = "Hungarian" });
            result.Add(new LanguageCode() { Code = "is", Name = "Icelandic" });
            result.Add(new LanguageCode() { Code = "id", Name = "Indonesian" });
            result.Add(new LanguageCode() { Code = "ga", Name = "Irish" });
            result.Add(new LanguageCode() { Code = "it", Name = "Italian" });
            result.Add(new LanguageCode() { Code = "ja", Name = "Japanese" });
            result.Add(new LanguageCode() { Code = "kn", Name = "Kannada" });
            result.Add(new LanguageCode() { Code = "ko", Name = "Korean" });
            result.Add(new LanguageCode() { Code = "lo", Name = "Lao" });
            result.Add(new LanguageCode() { Code = "la", Name = "Latin" });
            result.Add(new LanguageCode() { Code = "lv", Name = "Latvian" });
            result.Add(new LanguageCode() { Code = "lt", Name = "Lithuanian" });
            result.Add(new LanguageCode() { Code = "mk", Name = "Macedonian" });
            result.Add(new LanguageCode() { Code = "ms", Name = "Malay" });
            result.Add(new LanguageCode() { Code = "mt", Name = "Maltese" });
            result.Add(new LanguageCode() { Code = "no", Name = "Norwegian" });
            result.Add(new LanguageCode() { Code = "fa", Name = "Persian" });
            result.Add(new LanguageCode() { Code = "pl", Name = "Polish" });
            result.Add(new LanguageCode() { Code = "pt", Name = "Portuguese" });
            result.Add(new LanguageCode() { Code = "ro", Name = "Romanian" });
            result.Add(new LanguageCode() { Code = "ru", Name = "Russian" });
            result.Add(new LanguageCode() { Code = "sr", Name = "Serbian" });
            result.Add(new LanguageCode() { Code = "sk", Name = "Slovak" });
            result.Add(new LanguageCode() { Code = "sl", Name = "Slovenian" });
            result.Add(new LanguageCode() { Code = "es", Name = "Spanish" });
            result.Add(new LanguageCode() { Code = "sw", Name = "Swahili" });
            result.Add(new LanguageCode() { Code = "sv", Name = "Swedish" });
            result.Add(new LanguageCode() { Code = "ta", Name = "Tamil" });
            result.Add(new LanguageCode() { Code = "te", Name = "Telugu" });
            result.Add(new LanguageCode() { Code = "th", Name = "Thai" });
            result.Add(new LanguageCode() { Code = "tr", Name = "Turkish" });
            result.Add(new LanguageCode() { Code = "uk", Name = "Ukrainian" });
            result.Add(new LanguageCode() { Code = "ur", Name = "Urdu" });
            result.Add(new LanguageCode() { Code = "vi", Name = "Vietnamese" });
            result.Add(new LanguageCode() { Code = "cy", Name = "Welsh" });
            result.Add(new LanguageCode() { Code = "yi", Name = "Yiddish" });
            
            return result;
        }

        private void ParseFromGoogle()
        {
            try
            {
                var translateHelper = new GoogleTranslateEngine() { ToCulture = SelectedLanguageCode.Code };

                using (var resourceFileHelper = new ResourceFileHelper(this.SourceFile, this.TargetFile))
                {
                    var overwriteAll = false;
                    var cancelOperation = false;

                    foreach (var sourcePair in resourceFileHelper.GetAllNameValuesFromSource())
                    {
                        var translatedValue = translateHelper.TranslateWordOrPhrase(sourcePair.Value);
                        var existingTargetValue = resourceFileHelper.GetValueFromTargetUsingKey(sourcePair.Key);
                        




                        //  TODO: This WHOLE thing is terrible, PLEASE find a way to refactor it.
                        if (String.IsNullOrWhiteSpace(existingTargetValue) == false)
                        {
                            if (existingTargetValue.Equals(translatedValue, StringComparison.InvariantCultureIgnoreCase) == false)
                            {
                                if (overwriteAll)
                                    resourceFileHelper.WriteNameValuePairToTarget(sourcePair.Key, translatedValue, true);
                                else
                                    DispatchService.Invoke(() =>
                                        {
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
                                                    resourceFileHelper.WriteNameValuePairToTarget(sourcePair.Key, translatedValue, true);
                                                    break;
                                                case OverwriteResult.YesToAll:
                                                    resourceFileHelper.WriteNameValuePairToTarget(sourcePair.Key, translatedValue, true);
                                                    overwriteAll = true;
                                                    break;
                                                case OverwriteResult.Cancel:
                                                    MessageBox.Show("The translation operation has been aborted.", "Aborted",
                                                                    MessageBoxButton.OK, MessageBoxImage.Information);
                                                    cancelOperation = true;
                                                    break;
                                            }
                                        });
                            }
                        }
                        else
                            resourceFileHelper.WriteNameValuePairToTarget(sourcePair.Key, translatedValue, false);
                        //  TODO: This WHOLE thing is terrible, PLEASE find a way to refactor it.

                        if (cancelOperation) break;


                        //  TODO: Fix this to truly represent what happened.
                        DispatchService.Invoke(() => TranslatedItems.Add(String.Format("Translated English Key:'{0}' Value:'{1}' => '{2}'",
                                                                                            sourcePair.Key, sourcePair.Value, translatedValue)));
                    }
                }
            }
            catch(Exception ex)
            {
                View.DisplayMessageBox(String.Format("Description: {0}\n\nSource: {1}", ex.Message, ex.Source), "Error Occurred",
                                       MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
            }
        }

        private void ParseTranslationFile()
        {
            const int offset = 4;
            const int englishColumn = 1;
            const int translatedValueColumn = 2;
            
            var excelApp = new Microsoft.Office.Interop.Excel.Application();
            MessageBoxResult writeToAllKeysAnswer;

            try
            {
                var excelWb = excelApp.Workbooks.Open(this.TranslationFile, false, true);
                var workSheet = (Microsoft.Office.Interop.Excel.Worksheet)excelWb.Worksheets[1];
                var range = workSheet.UsedRange;
                
                using (var resourceFileHelper = new ResourceFileHelper(this.SourceFile, this.TargetFile))
                {
                    for (int rowIndex = offset; rowIndex <= range.Rows.Count; rowIndex++)
                    {
                        var englishValue = (range.Cells.Value2[rowIndex, englishColumn] ?? String.Empty).ToString().Trim().ToLower();
                        var translatedValue = (range.Cells.Value2[rowIndex, translatedValueColumn] ?? String.Empty).ToString().Trim();

                        if (String.IsNullOrWhiteSpace(englishValue) || String.IsNullOrWhiteSpace(translatedValue) || translatedValue.ToLower() == TRANSLATION_TO_SKIP)
                            continue;

                        Dictionary<string, string> sourceValues = resourceFileHelper.GetNameValuesFromSource(englishValue);
                        if (sourceValues.Any() == false) continue;

                        writeToAllKeysAnswer = MessageBoxResult.No;
                        if (sourceValues.Count() > 1)
                            writeToAllKeysAnswer = View.DisplayMessageBox(String.Format("The value \"{0}\" exists for multiple keys.\n\n", englishValue) +
                                                                          String.Join("\n", sourceValues.Select(v => String.Format("\tKey:{0} => Value:{1}", v.Key, v.Value))) + "\n\n" +
                                                                          String.Format("Use translation \"{0}\" for all keys?", translatedValue), "Use Translation For All?",
                                                                          MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.Yes);

                        if (writeToAllKeysAnswer == MessageBoxResult.Cancel)
                            break;

                        foreach (var sourcePair in sourceValues)
                        {
                            if (writeToAllKeysAnswer == MessageBoxResult.No)
                            {
                                //var overwriteDifferentAnswer = GetDifferentTranslationOverwriteAnswer(resourceFileHelper, sourcePair.Key, translatedValue);
                                var vm = new OverwriteWarningViewModel(new OverwriteWarning());
                                vm.View.ShowDialog();
                                if (vm.Answer == OverwriteResult.Cancel)
                                    break;
                            
                                resourceFileHelper.WriteNameValuePairToTarget(sourcePair.Key, translatedValue,
                                                                              (vm.Answer == OverwriteResult.Yes));
                            }
                            else
                                resourceFileHelper.WriteNameValuePairToTarget(sourcePair.Key, translatedValue, true);

                            View.Dispatcher.BeginInvoke(new Action(() => TranslatedItems.Add(String.Format("Translated English Key:'{0}' Value:'{1}' => '{2}'",
                                                                                                sourcePair.Key, sourcePair.Value, translatedValue))));
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
            }
            catch(Exception ex)
            {
                View.DisplayMessageBox(String.Format("Description: {0}\n\nSource: {1}", ex.Message, ex.Source), "Error Occurred",
                                       MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
            }
            finally
            {
                excelApp.Quit();
                Marshal.ReleaseComObject(excelApp);
                Marshal.FinalReleaseComObject(excelApp);
                excelApp = null;
            }
        }

        #endregion
    }
}
