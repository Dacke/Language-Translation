using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
using System.ComponentModel;
using System.Xml.Linq;
using Remotion.Data.Linq.Collections;


namespace TranslationHelper
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region Fields

        private ObservableCollection<String> translatedItems;

        private const string TRANSLATION_TO_SKIP = "(please inactivate)";

        #endregion

        #region Properties

        public string SourceFile { get; set; }
        public string TargetFile { get; set; }
        public string TranslationFile { get; set; }
        public ObservableCollection<String> TranslatedItems
        {
            get { return translatedItems; }
            private set
            {
                translatedItems = value;
                PropertyChanged(this, new PropertyChangedEventArgs("TranslatedItems"));
            }
        }
        public ObservableCollection<LanguageCode> LanguageCodes { get; private set; }

        #endregion

        #region Events

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        #endregion

        #region Public Methods

        public MainWindow()
        {
            this.TranslatedItems = new ObservableCollection<string>();
            this.LanguageCodes = FillLanguageCodes();

            InitializeComponent();

            lstStatus.DataContext = this;
        }

        #endregion

        #region Private Methods

        private ObservableCollection<LanguageCode> FillLanguageCodes()
        {
            var result = new ObservableCollection<LanguageCode>();

            result.Add(new LanguageCode() { Code = "af", Name = "Afrikaans" });
            result.Add(new LanguageCode() { Code = "sq", Name = "Albanian" }); 
            result.Add(new LanguageCode() { Code = "eu", Name = "Basque" });
            //result.Add(new LanguageCode() { Code = "zh-cn", Name = "Chinese (Simplified)" }); 
            result.Add(new LanguageCode() { Code = "nl", Name = "Dutch" });
            result.Add(new LanguageCode() { Code = "fr", Name = "French" });
            result.Add(new LanguageCode() { Code = "de", Name = "German" });
            result.Add(new LanguageCode() { Code = "it", Name = "Italian" });
            result.Add(new LanguageCode() { Code = "la", Name = "Latin" });
            result.Add(new LanguageCode() { Code = "no", Name = "Norwegian" });
            result.Add(new LanguageCode() { Code = "pl", Name = "Polish" }); 
            result.Add(new LanguageCode() { Code = "sv", Name = "Swedish" });
            result.Add(new LanguageCode() { Code = "es", Name = "Spanish" }); 
            
            return result;
        }

        private void OpenBrowseFileDialog<T>(string dialogTitle, string fileFilter, Expression<Func<T>> affectedProperty)
        {
            var fileDiag = new OpenFileDialog
                               {
                                   Title = dialogTitle,
                                   InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                                   Filter = fileFilter,
                                   RestoreDirectory = false
                               };

            var result = fileDiag.ShowDialog(this);
            if (result.Value == true)
            {
                var propName = GetPropertyName(affectedProperty);
                var propInfo = this.GetType().GetProperty(propName);
                propInfo.SetValue(this, fileDiag.FileName, null);
                OnPropertyChanged(propName);
            }
        }

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
            
        }

        private string GetPropertyName<T>(Expression<Func<T>> propertyLambda)
        {
            var memberExpression = propertyLambda.Body as MemberExpression;
            if (memberExpression == null)
                throw new ArgumentException("You must pass a lambda of the form: '() => Class.Property' or '() => object.Property'");

            return memberExpression.Member.Name;
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

                        //  Inspect the translation data from the Excel spread sheet
                        if (String.IsNullOrWhiteSpace(englishValue) || String.IsNullOrWhiteSpace(translatedValue) || translatedValue.ToLower() == TRANSLATION_TO_SKIP)
                            continue;

                        Dictionary<string, string> sourceValues = resourceFileHelper.GetNameValuesFromSource(englishValue);
                        if (sourceValues.Any() == false) continue;

                        writeToAllKeysAnswer = MessageBoxResult.No;
                        if (sourceValues.Count() > 1)
                            writeToAllKeysAnswer = MessageBox.Show(this, String.Format("The value \"{0}\" exists for multiple keys.\n\n", englishValue) +
                                                               String.Join("\n", sourceValues.Select(v => String.Format("\tKey:{0} => Value:{1}", v.Key, v.Value))) + "\n\n" +
                                                               String.Format("Use translation \"{0}\" for all keys?", translatedValue), "Use Translation For All?",
                                                         MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.Yes);

                        if (writeToAllKeysAnswer == MessageBoxResult.Cancel)
                            break;

                        foreach (var sourcePair in sourceValues)
                        {
                            if (writeToAllKeysAnswer == MessageBoxResult.No)
                            {
                                var overwriteDifferentAnswer = GetDifferentTranslationOverwriteAnswer(resourceFileHelper, sourcePair.Key, translatedValue);
                                if (overwriteDifferentAnswer == MessageBoxResult.Cancel)
                                    break;
                            
                                resourceFileHelper.WriteNameValuePairToTarget(sourcePair.Key, translatedValue,
                                                                              (overwriteDifferentAnswer == MessageBoxResult.Yes));
                            }
                            else
                                resourceFileHelper.WriteNameValuePairToTarget(sourcePair.Key, translatedValue, true);

                            TranslatedItems.Add(String.Format("Translated English Key:'{0}' Value:'{1}' => '{2}'",
                                                               sourcePair.Key, sourcePair.Value, translatedValue));
                            OnPropertyChanged("TranslatedItems");
                        }
                    }

                    excelWb.Close(false, Type.Missing, Type.Missing);
                    excelApp.Workbooks.Close();
                    Marshal.ReleaseComObject(range);
                    Marshal.ReleaseComObject(excelWb);
                    range = null;
                    workSheet = null;
                    excelWb = null;

                    MessageBox.Show("The translation has successfully be parsed.  Please check the output window for a list of items that have been translated.", "Success",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
                if (Debugger.IsAttached)
                    Debugger.Break();
            }
            finally
            {
                excelApp.Quit();
                Marshal.ReleaseComObject(excelApp);
                Marshal.FinalReleaseComObject(excelApp);
                excelApp = null;
            }
        }

        private MessageBoxResult GetDifferentTranslationOverwriteAnswer(ResourceFileHelper resourceFileHelper, string resourceKey, string translatedValue)
        {
            MessageBoxResult answer = MessageBoxResult.Yes;

            var targetValue = resourceFileHelper.GetValueFromTargetUsingKey(resourceKey);
            if (String.IsNullOrWhiteSpace(targetValue) == false)
            {
                if (targetValue != translatedValue)
                {
                    //  Different Translation already exists overwrite?
                    answer = MessageBox.Show(this, "A different translation already exists in the target file\n\n" +
                                                    String.Format("Existing Value\t: {0}\nNew Value\t: {1}\n\n", targetValue, translatedValue) +
                                                    "Do you want to overwrite this value?", "Overwrite?",
                                                MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.No);
                }
            }            

            return answer;
        }

        private void ParseFromGoogle()
        {            
            var translateHelper = new GoogleTranslateHelper()
                                      {
                                          ToCulture = ((LanguageCode)comboLanguageCode.SelectedValue).Code
                                      };

            using (var resourceFileHelper = new ResourceFileHelper(this.SourceFile, this.TargetFile))
            {
                foreach (var sourcePair in resourceFileHelper.GetAllNameValuesFromSource())
                {
                    var translatedValue = translateHelper.TranslateWordOrPhrase(sourcePair.Value);
                    var existingTargetValue = resourceFileHelper.GetValueFromTargetUsingKey(sourcePair.Key);
                    if (String.IsNullOrWhiteSpace(existingTargetValue) == false)
                    {
                        if (existingTargetValue.Equals(translatedValue, StringComparison.InvariantCultureIgnoreCase) == false)
                        {
                            MessageBoxResult answer = MessageBox.Show(this, "A translation already exists in the target file\n\n" +
                                                               String.Format("Existing Value\t: {0}\nNew Value\t: {1}\n\n", existingTargetValue, translatedValue) +
                                                               "Do you want to overwrite this value?", "Overwrite?",
                                                         MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.No);
                            switch (answer)
                            {
                                case MessageBoxResult.Yes:
                                    resourceFileHelper.WriteNameValuePairToTarget(sourcePair.Key, translatedValue, true);
                                    break;
                                case MessageBoxResult.Cancel:
                                    MessageBox.Show("The translation operation has been aborted.", "Aborted", MessageBoxButton.OK, MessageBoxImage.Information);
                                    return;
                            }
                        }
                    }
                    else
                        resourceFileHelper.WriteNameValuePairToTarget(sourcePair.Key, translatedValue, false);

                    TranslatedItems.Add(String.Format("Translated English Key:'{0}' Value:'{1}' => '{2}'",
                                                      sourcePair.Key, sourcePair.Value, translatedValue));
                    OnPropertyChanged("TranslatedItems");                                                
                }
            }

            MessageBox.Show("The translation has successfully be done from Google.  Please check the output window for a list of items that have been translated.", "Success",
                                                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #region Event Handlers
        
        // ReSharper disable InconsistentNaming

        private void cmdSourceBrowse_Click(object sender, RoutedEventArgs e)
        {
            const string dialogTitle = "Select English File";
            const string fileFilter = "Resource Files(*.resx)|*.resx|All Files(*.*)|*.*";

            OpenBrowseFileDialog(dialogTitle, fileFilter, () => this.SourceFile);
        }
        
        private void cmdTargetBrowse_Click(object sender, RoutedEventArgs e)
        {
            const string dialogTitle = "Select Target File";
            const string fileFilter = "Resource Files(*.resx)|*.resx|All Files(*.*)|*.*";

            OpenBrowseFileDialog(dialogTitle, fileFilter, () => this.TargetFile);
        }

        private void chkTranslateFromGoogle_Click(object sender, RoutedEventArgs e)
        {
            txtTranslationsFile.IsEnabled = (bool) (!chkTranslateFromGoogle.IsChecked);
            cmdTranslationsBrowse.IsEnabled = (bool) (!chkTranslateFromGoogle.IsChecked);
            comboLanguageCode.IsEnabled = (bool) (chkTranslateFromGoogle.IsChecked);
        }

        private void cmdTranslationsBrowse_Click(object sender, RoutedEventArgs e)
        {
            const string dialogTitle = "Select Translation File";
            const string fileFilter = "Excel SpreadSheet|*.xls;*.xlsx|All Files(*.*)|*.*";

            OpenBrowseFileDialog(dialogTitle, fileFilter, () => this.TranslationFile);
        }

        private void cmdTranslate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                #region Argument Validation

                if (String.IsNullOrWhiteSpace(this.SourceFile) 
                    || this.SourceFile.EndsWith("resx") == false)
                    throw new Exception("You have not filled in a value for the English String Resource File. (*.resx)");

                if (String.IsNullOrWhiteSpace(this.TargetFile)
                    || this.TargetFile.EndsWith("resx") == false)
                    throw new Exception("You have not filled in a value for the Target Resource File. (*.resx)");

                if (chkTranslateFromGoogle.IsChecked != true)
                {
                    if (String.IsNullOrWhiteSpace(this.TranslationFile)
                        || (this.TranslationFile.EndsWith("xls") == false & this.TranslationFile.EndsWith("xlsx") == false))
                        throw new Exception("You have not filled in a value for the Translations File. (*.xls, *.xlsx)");
                }

                #endregion

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (this.TranslatedItems.Count > 0)
                        this.TranslatedItems.Clear();
                    lstStatus.Items.Refresh();
                }));

                Dispatcher.BeginInvoke(new Action(() =>
                                                      {
                                                          Cursor = Cursors.Wait;

                                                          if (chkTranslateFromGoogle.IsChecked != true)
                                                              this.ParseTranslationFile();
                                                          else
                                                              this.ParseFromGoogle();

                                                          Cursor = Cursors.Arrow;
                                                      }));
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
                if (Debugger.IsAttached)
                    Debugger.Break();
                MessageBox.Show(ex.Message, ex.Source, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
       
        // ReSharper restore InconsistentNaming

        #endregion

        #endregion
    }
}
