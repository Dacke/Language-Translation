using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TranslationHelper
{
    /// <summary>
    /// Interaction logic for OverwriteWarning.xaml
    /// </summary>
    public partial class OverwriteWarning : Window
    {
        public MessageBoxResult Answer { get; private set; }

        public OverwriteWarning()
        {
            InitializeComponent();
            Answer = MessageBoxResult.Yes;
        }
    }
}
