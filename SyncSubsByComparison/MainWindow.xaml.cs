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
using System.Windows.Navigation;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Research.DynamicDataDisplay.DataSources;
using Microsoft.Research.DynamicDataDisplay;
namespace SyncSubsByComparison
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private MainVM myViewModel;

        public MainVM ViewModel
        {
            get
            {
                return ((MainVM)DataContext);
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            plotter.AddLineGraph(ViewModel.ActualData, 1, "Sync Difference");
            plotter.AddLineGraph(ViewModel.BaselineData, 2, "Baseline");
            plotter.AddLineGraph(ViewModel.RegressionData, 2, "L.Regression");
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.AutoSyncSubtitles();
            plotter.IsEnabled = true;
        }

        //private void button2_Click(object sender, RoutedEventArgs e)
        //{
        //    //string lang=  BingTranslator.DetectLanguage("Dror had some pie");
        //    //string translated = BingTranslator.Translate("דרור אכל גלידה", "en");
        //}

        WndPasteTranslation _wndTranslation = new WndPasteTranslation();
        
        /// <summary>
        /// add translation from google translate
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click(object sender, RoutedEventArgs e)
        {
            _wndTranslation.TranslationText = ViewModel.TranslationText;
            _wndTranslation.ShowDialog();
            ViewModel.TranslationText = _wndTranslation.TranslationText;

        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            string newSrtFile = Path.GetDirectoryName(txtLanguageSrt.Text) + "\\fixed_" + Path.GetFileName(txtLanguageSrt.Text);

            if (File.Exists(newSrtFile))
                File.Delete(newSrtFile);

            ViewModel.FixedSub.WriteSrt(newSrtFile);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //workaround some bug of the graph component (application won't close properly)
            Application.Current.Shutdown();
        }

        //update graph
        private void button1_Click_1(object sender, RoutedEventArgs e)
        {
            plotter.IsEnabled = true;
            ViewModel.SyncSubtitles();
        }

    
    }
}
