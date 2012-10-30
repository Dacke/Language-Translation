using System;
using System.Linq;
using System.Linq.Expressions;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;

namespace TranslationHelper
{
    public interface ITranslationHelperView
    {
        void SetModel(TranslationHelperViewModel model);
        void OpenBrowseFileDialog(string dialogTitle, string fileFilter, Expression<Func<TranslationHelperViewModel, object>> property);
        void SetApplicationCursor(Cursor cursor);
        void AddOutputString(string message);
        MessageBoxResult DisplayMessageBox(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult);
        void ScrollOutput();
    }

    public partial class TranslationHelperView : ITranslationHelperView
    {
        public TranslationHelperViewModel Model { get { return DataContext as TranslationHelperViewModel; } }

        public TranslationHelperView()
        {
            InitializeComponent();
        }

        public void SetModel(TranslationHelperViewModel model)
        {
            DataContext = model;
        }

        public void OpenBrowseFileDialog(string dialogTitle, string fileFilter, Expression<Func<TranslationHelperViewModel, object>> affectedProperty)
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
                var propName = affectedProperty.GetPropertyName();
                var propInfo = this.DataContext.GetType().GetProperty(propName);
                propInfo.SetValue(this.DataContext, fileDiag.FileName, null);
            }
        }

        public void SetApplicationCursor(Cursor cursor)
        {
            Dispatcher.BeginInvoke(new Action(() => Cursor = cursor));
        }

        public void AddOutputString(string message)
        {
            Dispatcher.BeginInvoke(new Action(() => Model.TranslatedItems.Add(message)));
        }

        public MessageBoxResult DisplayMessageBox(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult)
        {
            return Dispatcher.Invoke(() => MessageBox.Show(this, messageBoxText, caption, button, icon, defaultResult));
        }

        public void ScrollOutput()
        {
            var item = Model.TranslatedItems.LastOrDefault();
            if (String.IsNullOrWhiteSpace(item) == false)
                Dispatcher.BeginInvoke(new Action(() => lstStatus.ScrollIntoView(item)));
        }
    }
}
