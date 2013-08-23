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
using Microsoft.Research.DynamicDataDisplay.Charts.Navigation;
using Microsoft.Research.DynamicDataDisplay.PointMarkers;
using System.Windows.Controls.Primitives;
namespace SyncSubsByComparison
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        int _selectedPointIndex = int.MinValue;

        public MainVM ViewModel
        {
            get
            {
                return ((MainVM)DataContext);
            }
        }

        CursorCoordinateGraph cursorCoordinateGraph = new CursorCoordinateGraph();
        LineAndMarker<MarkerPointsGraph> _editableGraph = null;
        List<DependencyObject> hitTestList = new List<DependencyObject>();
        WndPasteTranslation _wndTranslation = new WndPasteTranslation();

        public MainWindow()
        {
            InitializeComponent();
            cursorCoordinateGraph.Visibility = System.Windows.Visibility.Hidden;
            plotter.Children.Add(cursorCoordinateGraph);

            plotter.AddLineGraph(ViewModel.RegressionData, new Pen(Brushes.LightGreen, 2), new PenDescription("L.Regression"));

            _editableGraph = plotter.AddLineGraph(ViewModel.ActualData,
                                                    new Pen(Brushes.Violet, 2),
                                                    new CirclePointMarker
                                                    {
                                                        Size = 6,
                                                        Fill = Brushes.Violet,
                                                        Pen = new Pen(Brushes.BlueViolet, 2),
                                                    },
                                                    new PenDescription("Sync Difference"));

            plotter.AddLineGraph(ViewModel.BaselineData, new Pen(Brushes.DodgerBlue, 2), new PenDescription("Baseline"));
        }

        /// <summary>
        /// auto sync
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.AutoSyncSubtitles();
            plotter.IsEnabled = true;
        }

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

        /// <summary>
        /// save to file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            //var yTest = ViewModel.SelectedLineForSubtitleFix.ComputeYforXbyInterpolation(230000);
            string newSrtFile = Path.GetDirectoryName(txtLanguageSrt.Text) + "\\fixed_" + Path.GetFileName(txtLanguageSrt.Text);

            if (File.Exists(newSrtFile))
                File.Delete(newSrtFile);

            ViewModel.FixedSub.WriteSrt(newSrtFile);
        }

        /// <summary>
        /// closing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //workaround some bug of the graph component (application won't close properly)
            Application.Current.Shutdown();
        }

        /// <summary>
        /// update graph button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click_1(object sender, RoutedEventArgs e)
        {
            plotter.IsEnabled = true;
            ViewModel.SyncSubtitles();
        }

        private void plotter_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _selectedPointIndex = int.MinValue;
            plotter.Cursor = Cursors.Arrow;
            EditorInfoPopup.IsOpen = false;
        }

        private void plotter_MouseMove(object sender, MouseEventArgs e)
        {

            if (_selectedPointIndex != int.MinValue)
            {
                var pos = e.GetPosition(cursorCoordinateGraph);
                
                var data = plotter.Viewport.Transform.ScreenToData(pos);

                var dataSource = ((ObservableDataSource<Point>)_editableGraph.LineGraph.DataSource);
                var ptx = dataSource.Collection[_selectedPointIndex].X;
                dataSource.Collection[_selectedPointIndex] = new Point(ptx, data.Y);
                ViewModel.UpdateEditableLine(_selectedPointIndex, ptx, data.Y);
                EditorInfoPopup.Placement = PlacementMode.Mouse;
                PopupText.Text = ViewModel.GetTextForPoint(new Point(ptx, data.Y));
            }
            else
            {
                Point position = e.GetPosition(plotter);
                hitTestList.Clear();
                VisualTreeHelper.HitTest(plotter, null, CollectAllVisuals_Callback, new PointHitTestParameters(position));

                if (hitTestList.Contains(_editableGraph.LineGraph))
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
            var dataSource = (ObservableDataSource<Point>)_editableGraph.LineGraph.DataSource;
            double distClosestPoint = dataSource.Collection.Min(p => Math.Pow(p.X - data.X, 2) + Math.Pow(p.Y - data.Y, 2));

            if (plotter.Cursor == Cursors.Hand)
            {
                e.Handled = true;

                Point pt = dataSource.Collection.Where(p => Math.Pow(p.X - data.X, 2) + Math.Pow(p.Y - data.Y, 2) == distClosestPoint).FirstOrDefault();
                _selectedPointIndex = dataSource.Collection.IndexOf(pt);

                PopupText.Text = ViewModel.GetTextForPoint(pt);
                EditorInfoPopup.IsOpen = true;

                if (hitTestList.Contains(_editableGraph.LineGraph))
                {
                    // Set cursor shape
                    plotter.Cursor = Cursors.SizeNS;
                }
            }

        }

        private void EditorInfoPopup_MouseEnter(object sender, MouseEventArgs e)
        {
            EditorInfoPopup.ClearValue(Popup.IsOpenProperty);
            EditorInfoPopup.IsOpen = true;
        }

        HitTestResultBehavior CollectAllVisuals_Callback(HitTestResult result)
        {
            if (result == null || result.VisualHit == null)
                return HitTestResultBehavior.Stop;

            hitTestList.Add(result.VisualHit);
            return HitTestResultBehavior.Continue;
        }
    }
}
