using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SyncSubsByComparison
{
    /// <summary>
    /// Interaction logic for WndPasteTranslation.xaml
    /// </summary>
    public partial class WndPasteTranslation : Window
    {
        public WndPasteTranslation()
        {
            InitializeComponent();
        }

        public string TranslationText { get { return textBlock1.Text; } set { textBlock1.Text = value; } }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

    }
}
