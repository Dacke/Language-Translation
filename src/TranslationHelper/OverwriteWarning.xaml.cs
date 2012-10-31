using System.Windows;

namespace TranslationHelper
{
    public interface IOverwriteWarning
    {
        void SetModel(OverwriteWarningViewModel model);
        void Show();
        void Close();
        bool? ShowDialog();
    }

    public partial class OverwriteWarning : Window, IOverwriteWarning
    {
        public OverwriteWarningViewModel Model { get { return DataContext as OverwriteWarningViewModel; } }

        public OverwriteWarning()
        {
            InitializeComponent();
        }

        public void SetModel(OverwriteWarningViewModel model)
        {
            DataContext = model;
        }
    }
}
