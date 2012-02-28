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
        #region Constants

        private const string rootElement = "root";
        private const string dataElement = "data";
        private const string dataNameAttribute = "name";
        private const string valueElement = "value";

        #endregion

        #region Properties

        public string SourceFile { get; set; }
        public string TargetFile { get; set; }
        public string TranslationFile { get; set; }
        public ObservableCollection<string> TranslatedItems { get; private set; }
        public ObservableCollection<LanguageCode> LanguageCodes { get; private set; }

        #endregion

        #region Events

        public event PropertyChangedEventHandler PropertyChanged;

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
            const string translationToSkip = "(please inactivate)";

            var excelApp = new Microsoft.Office.Interop.Excel.Application();

            try
            {
                var xDocSource = XDocument.Load(this.SourceFile);
                var xDocTarget = XDocument.Load(this.TargetFile);
                var excelWb = excelApp.Workbooks.Open(this.TranslationFile, false, true);
                var workSheet = (Microsoft.Office.Interop.Excel.Worksheet)excelWb.Worksheets[1];
                var range = workSheet.UsedRange;
                var dirtyFile = false;

                for (int rowIndex = offset; rowIndex <= range.Rows.Count; rowIndex++)
                {
                    var englishValue = (range.Cells.Value2[rowIndex, englishColumn] ?? String.Empty).ToString().Trim().ToLower();
                    var translatedValue = (range.Cells.Value2[rowIndex, translatedValueColumn] ?? String.Empty).ToString().Trim();

                    //  Inspect the translation data from the Excel spread sheet
                    if (String.IsNullOrWhiteSpace(englishValue) || String.IsNullOrWhiteSpace(translatedValue) || translatedValue.ToLower() == translationToSkip)
                        continue;

                    //  Pull the information from the source resource file.
                    var sourceValue =
                        xDocSource.Element(rootElement).Elements(dataElement).FirstOrDefault(se => se.Element(valueElement).Value.Trim().ToLower() == englishValue);
                    if (sourceValue == null) continue;

                    //  Get the data attribute.
                    var dataAttributeValue = sourceValue.Attribute(dataNameAttribute).Value;
                    if (String.IsNullOrWhiteSpace(dataAttributeValue))
                        continue;

                    var targetValue = xDocTarget.Element(rootElement).Elements(dataElement)
                        .FirstOrDefault(se => se.Attribute(dataNameAttribute).Value.Trim().ToLower() == dataAttributeValue.Trim().ToLower());
                    if (targetValue != null)
                    {
                        var existingTargetValue = targetValue.Element(valueElement).Value;

                        if (existingTargetValue != translatedValue)
                        {
                            //  Translation already exists overwrite?
                            MessageBoxResult answer = MessageBox.Show(this, "A translation already exists in the target file\n\n" +
                                                                            String.Format("Existing Value\t: {0}\nNew Value\t: {1}\n\n", existingTargetValue, translatedValue) +
                                                                            "Do you want to overwrite this value?", "Overwrite?",
                                                                      MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.No);
                            switch (answer)
                            {
                                case MessageBoxResult.Yes:
                                    targetValue.Element(valueElement).Value = translatedValue;
                                    dirtyFile = true;
                                    break;
                                case MessageBoxResult.Cancel:
                                    MessageBox.Show("The translation operation has been aborted.", "Aborted", MessageBoxButton.OK, MessageBoxImage.Information);
                                    return;
                            }
                        }
                        else
                            continue;
                    }
                    else
                    {
                        xDocTarget.Element(rootElement).Add(new XElement(dataElement,
                                                                         new XAttribute(dataNameAttribute, dataAttributeValue),
                                                                         new XAttribute(XNamespace.Xml + "space", "preserve"),
                                                                         new XElement(valueElement, translatedValue)));
                        dirtyFile = true;
                    }

                    Dispatcher.BeginInvoke(new Action(() =>
                                                          {
                                                              TranslatedItems.Add(
                                                                  String.Format("Translated English Key:'{0}' Value:'{1}' => '{2}'", dataAttributeValue, englishValue,
                                                                                translatedValue));
                                                              lstStatus.Items.Refresh();
                                                          }));
                }
                
                excelWb.Close(false, Type.Missing, Type.Missing);
                excelApp.Workbooks.Close();
                Marshal.ReleaseComObject(range);
                Marshal.ReleaseComObject(excelWb);
                range = null;
                workSheet = null; 
                excelWb = null;

                if (dirtyFile)
                    xDocTarget.Save(TargetFile);

                MessageBox.Show("The translation has successfully be parsed.  Please check the output window for a list of items that have been translated.", "Success",
                                MessageBoxButton.OK, MessageBoxImage.Information);
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

        private void ParseFromGoogle()
        {
            var translateHelper = new GoogleTranslateHelper()
                                      {
                                          ToCulture = ((LanguageCode)comboLanguageCode.SelectedValue).Code
                                      };

            var xDocSource = XDocument.Load(this.SourceFile);
            var xDocTarget = XDocument.Load(this.TargetFile);
            var dirtyFile = false;

            //  Iterate through the source document
            foreach (var sourceValue in xDocSource.Element(rootElement).Elements(dataElement))
            {
                //  Get the data attribute.
                var dataAttributeValue = sourceValue.Attribute(dataNameAttribute).Value;
                if (String.IsNullOrWhiteSpace(dataAttributeValue))
                    continue;

                var englishValue = sourceValue.Element(valueElement).Value;

                //  Translate the value
                var translatedValue = translateHelper.TranslateWordOrPhrase(englishValue);

                var targetValue = xDocTarget.Element(rootElement).Elements(dataElement)
                    .FirstOrDefault(se => se.Attribute(dataNameAttribute).Value.Trim().ToLower() == dataAttributeValue.Trim().ToLower());
                if (targetValue != null)
                {
                    var existingTargetValue = targetValue.Element(valueElement).Value;

                    if (existingTargetValue != translatedValue)
                    {
                        //  Translation already exists overwrite?
                        MessageBoxResult answer = MessageBox.Show(this, "A translation already exists in the target file\n\n" +
                                                           String.Format("Existing Value\t: {0}\nNew Value\t: {1}\n\n", existingTargetValue, translatedValue) +
                                                           "Do you want to overwrite this value?", "Overwrite?",
                                                     MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.No);
                        switch (answer)
                        {
                            case MessageBoxResult.Yes:
                                targetValue.Element(valueElement).Value = translatedValue;
                                dirtyFile = true;
                                break;
                            case MessageBoxResult.Cancel:
                                MessageBox.Show("The translation operation has been aborted.", "Aborted", MessageBoxButton.OK, MessageBoxImage.Information);
                                return;
                        }
                    }
                    else
                        continue;
                }
                else
                {
                    xDocTarget.Element(rootElement).Add(new XElement(dataElement,
                                                                     new XAttribute(dataNameAttribute, dataAttributeValue),
                                                                     new XAttribute(XNamespace.Xml + "space", "preserve"),
                                                                     new XElement(valueElement, translatedValue)));
                    dirtyFile = true;
                }

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    TranslatedItems.Add(
                        String.Format("Translated English Key:'{0}' Value:'{1}' => '{2}'", dataAttributeValue, englishValue,
                                      translatedValue));
                    lstStatus.Items.Refresh();
                }));
            }

            if (dirtyFile)
                xDocTarget.Save(TargetFile);

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

                if (String.IsNullOrWhiteSpace(this.TranslationFile) 
                    || (this.TranslationFile.EndsWith("xls") == false & this.TranslationFile.EndsWith("xlsx") == false))
                    throw new Exception("You have not filled in a value for the Translations File. (*.xls, *.xlsx)");

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
