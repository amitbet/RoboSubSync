﻿using System;
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
using Microsoft.Research.DynamicDataDisplay.Charts.Navigation;
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

        CursorCoordinateGraph cursorCoordinateGraph = new CursorCoordinateGraph();
        LineGraph _editableGraph = null;
        public MainWindow()
        {
            InitializeComponent();
            //cursorCoordinateGraph.Visibility = System.Windows.Visibility.Hidden;
            plotter.Children.Add(cursorCoordinateGraph);

            _editableGraph = plotter.AddLineGraph(ViewModel.ActualData, 1, "Sync Difference");
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

        private void plotter_IsMouseDirectlyOverChanged(object sender, DependencyPropertyChangedEventArgs e)
        {

        }

        int _selectedPointIndex = int.MinValue;
        private void plotter_MouseDown(object sender, MouseButtonEventArgs e)
        {
        }

        private void plotter_MouseUp(object sender, MouseButtonEventArgs e)
        {
            {
                //Point p = ((Visual)sender).PointToScreen(Mouse.GetPosition((IInputElement)sender));
                //var a = plotter.Viewport.Transform.ScreenToData(p);
                //var pos = e.GetPosition(cursorCoordinateGraph);
                //var scrPoint = plotter.Viewport.Transform.ViewportToScreen(pos);
                //var data = plotter.Viewport.Transform.ScreenToData(pos);

                _selectedPointIndex = int.MinValue;
                plotter.Cursor = Cursors.Arrow;
            }
        }

        HitTestResultBehavior CollectAllVisuals_Callback(HitTestResult result)
        {
            if (result == null || result.VisualHit == null)
                return HitTestResultBehavior.Stop;

            hitTestList.Add(result.VisualHit);
            return HitTestResultBehavior.Continue;
        }

        List<DependencyObject> hitTestList = new List<DependencyObject>();
        private void plotter_MouseMove(object sender, MouseEventArgs e)
        {
            if (_selectedPointIndex != int.MinValue)
            {
                var pos = e.GetPosition(cursorCoordinateGraph);
                var data = plotter.Viewport.Transform.ScreenToData(pos);

                var dataSource = ((ObservableDataSource<Point>)_editableGraph.DataSource);
                var ptx = dataSource.Collection[_selectedPointIndex].X;
                dataSource.Collection[_selectedPointIndex] = new Point(ptx, data.Y);


            }
            else
            {
                Point position = e.GetPosition(plotter);
                hitTestList.Clear();
                VisualTreeHelper.HitTest(plotter, null, CollectAllVisuals_Callback, new PointHitTestParameters(position));

                if (hitTestList.Contains(_editableGraph))
                {
                    plotter.Cursor = Cursors.Hand;
                }
                else
                {
                    plotter.Cursor = Cursors.Arrow;
                }
            }
        }

        private void plotter_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {

            // Call Hit Test Method
            Point position = e.GetPosition(plotter);
            hitTestList.Clear();
            VisualTreeHelper.HitTest(plotter, null, CollectAllVisuals_Callback, new PointHitTestParameters(position));
            var pos = e.GetPosition(cursorCoordinateGraph);
            var data = plotter.Viewport.Transform.ScreenToData(pos);
            var dataSource = (ObservableDataSource<Point>)_editableGraph.DataSource;
            double distClosestX = dataSource.Collection.Min(p => Math.Abs(p.X - data.X));

            if (plotter.Cursor != Cursors.Hand)
                return;

            e.Handled = true;

            Point pt = dataSource.Collection.Where(p => Math.Abs(p.X - data.X) == distClosestX).FirstOrDefault();
            _selectedPointIndex = dataSource.Collection.IndexOf(pt);


            ////dataSource.Collection[_selectedPointIndex] = new Point(pt.X, pt.Y + 1000);

            //dataSource.Collection.Add(new Point(pt.X, pt.Y + 1000));

            //ViewModel.EditTargetGraph(new Point(pt.X, pt.Y + 1000), dataSource);

            //var newsource = new ObservableDataSource<Point>();
            //newsource.AppendMany(dataSource.Collection);
            //_editableGraph.DataSource = newsource;

            //// Initialize currently selected data point
            //selectedDataPoint = null;
            if (hitTestList.Contains(_editableGraph))
            {
                //    selectedDataPoint = (DataPoint)hitResult.Object;

                //    // Show point value as label
                //    selectedDataPoint.IsValueShownAsLabel = true;

                // Set cursor shape
                plotter.Cursor = Cursors.SizeNS;
            }

        }


    }
}
