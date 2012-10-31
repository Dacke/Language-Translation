using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace TranslationHelper
{
    public class OverwriteWarningViewModel
    {
        public string IconImage { get; set; }
        public string ExistingValueLabel { get { return "Existing:"; } }
        public string TranslationLabel { get { return "Translation:"; } }
        public string YesLabel { get { return "Yes"; } }
        public string YesToAllLabel { get { return "Yes To All"; } }
        public string NoLabel { get { return "No"; } }
        public string CancelLabel { get { return "Cancel"; } }

        public OverwriteResult Answer { get; set; }
        public IOverwriteWarning View { get; set; }
        public string Description { get; set; }
        public string Question { get; set; }
        public string ExistingValue { get; set; }
        public string TranslationValue { get; set; }

        public ICommand YesCommand { get; set; }
        public ICommand YesToAllCommand { get; set; }
        public ICommand NoCommand { get; set; }
        public ICommand CancelCommand { get; set; }

        public OverwriteWarningViewModel(IOverwriteWarning view)
        {

            YesCommand = new DelegateCommand(m => OnSetPropertyAndExit(OverwriteResult.Yes));
            YesToAllCommand = new DelegateCommand(m => OnSetPropertyAndExit(OverwriteResult.YesToAll));
            NoCommand = new DelegateCommand(m => OnSetPropertyAndExit(OverwriteResult.No));
            CancelCommand = new DelegateCommand(m => OnSetPropertyAndExit(OverwriteResult.Cancel));

            View = view;
            View.SetModel(this);
        }

        private void OnSetPropertyAndExit(OverwriteResult overwriteResult)
        {
            Answer = overwriteResult;
            View.Close();
        }
    }
}
