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
using System.Windows.Shapes;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace SyncSubsByComparison
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {


        public MainWindow()
        {
            InitializeComponent();
        }


        private void button1_Click(object sender, RoutedEventArgs e)
        {
            SubtitleInfo langSub = new SubtitleInfo();
            SubtitleInfo timingSub = new SubtitleInfo();

            langSub.LoadSrtFile(txtLanguageSrt.Text);
            timingSub.LoadSrtFile(txtTimingSrt.Text);

            Dictionary<LineInfo, LineInfo> matchedLangLines2timingLines = new Dictionary<LineInfo, LineInfo>();
            int timingSrtPos = 0;

            //start a bit after the beginning to skip unwanted chatter lines
            for (int i = 0; i < langSub.Lines.Count(); ++i)
            {
                var sline = langSub.Lines[i];
                int endSearch = Math.Min(timingSub.Lines.Count(), timingSrtPos + Contants.LinesToSearchForward);
                for (int j = timingSrtPos; j < endSearch; ++j)
                {
                    var tline = timingSub.Lines[j];

                    //if we have a match
                    if (tline.CalcIsWordMatch(sline))
                    {
                        if (!matchedLangLines2timingLines.ContainsKey(sline))
                            matchedLangLines2timingLines.Add(sline, tline);
                        timingSrtPos = j + 1;
                    }
                }
            }
            var ordered = matchedLangLines2timingLines.OrderBy(x => x.Key.TimeStamp.FromTime);
            var lastTimeStamForSync = ordered.Last().Value.TimeStamp.FromTime;
            var dataset = ordered.Select(x => new { diff = (x.Value.TimeStamp.FromTime - x.Key.TimeStamp.FromTime), origTime = x.Value.TimeStamp.FromTime }).ToList();
            var dataset2 = ordered.Select(x => x.Value.TimeStamp.FromTime - x.Key.TimeStamp.FromTime).ToList();
            string strDataset = dataset.Select(x => x.ToString()).Aggregate((x, y) => x + " " + y);
            var averages = CreateBaseline(dataset2, 20, 40, 0.01d,3);
            var strAverages = averages.Select(x => x.ToString()).Aggregate((x, y) => x + " " + y);
            //for (int i = 1; i < dataset.Count(); ++i)
            //{
            //    dataset[i].diff > 3*dataset[i-1].diff && ;

            //}
            //int windowSize = 3;
            //double ampLimit = 1.2d;
            //int smoothingIterations = 5;

            //for (int i = 0; i < smoothingIterations; ++i)
            //{
            //    dataset = dataset.Where((x, index) => (index > windowSize && index < dataset.Count() - windowSize) ?
            //        Math.Abs(x) <= Math.Abs(ampLimit * (dataset.Where((a, idx) => (idx >= index - windowSize && idx <= index - 1)).Sum()) / windowSize) ||
            //        Math.Abs(x) <= Math.Abs(ampLimit * (dataset.Where((a, idx) => (idx >= index + 1 && idx <= index + windowSize)).Sum()) / windowSize) : true).ToList();
            //}

            //string strDataset1 = dataset.Select(x => x.ToString()).Aggregate((x, y) => x + "," + y);
        }

        double CalcSumOfSquares(IEnumerable<long> series, double average)
        {
            double sumOfSquares = 0;
            foreach (var item in series)
            {
                sumOfSquares += Math.Pow(item - average, 2);
            }
            return sumOfSquares;
        }

        double CalcAverage(IEnumerable<long> series)
        {
            return ((double)series.Aggregate((a, b) => a + b) / (double)series.Count());
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="series"></param>
        /// <param name="numErrorsToBacktrackOn"></param>
        /// <param name="initialSectionLength"></param>
        /// <param name="algAlpha">the forgetfullness constant</param>
        /// <param name="normalcyAmplitudeConstant"></param>
        /// <returns></returns>
        public List<double> CreateBaseline(List<long> series, int numErrorsToBacktrackOn, int initialSectionLength, double algAlpha = 0.01, int normalcyAmplitudeConstant = 3)
        {

            var leadIn = series.Take(initialSectionLength);
            double average = CalcAverage(leadIn);
            double sumOfSquares = CalcSumOfSquares(leadIn, average);
            //double algAlpha = 0.02d;
            double alpha = algAlpha;
            int errorCount = 0;



            List<long> abnormalValuePositions = new List<long>();
            List<double> averageValues = new List<double>();
            for (int i = initialSectionLength; i < series.Count(); ++i)
            {
                var sample = series[i];
                var stdDeviation = CalcStddev(sumOfSquares, average);
                double distanceFromAvg = Math.Abs(sample - average);

                //an abnormal value is found
                if (distanceFromAvg > 3 * stdDeviation)
                {
                    alpha = Math.Pow(algAlpha, (distanceFromAvg / (stdDeviation)) - 2);

                    ++errorCount;
                    abnormalValuePositions.Add(i);

                    //if too many errors occured, go back to where we lost it, and clear the averages, so we start fresh.
                    if (errorCount >= numErrorsToBacktrackOn)
                    {
                        //release all errors tags in list until the rollback point
                        abnormalValuePositions.RemoveRange(abnormalValuePositions.Count - numErrorsToBacktrackOn, numErrorsToBacktrackOn);
                        averageValues.RemoveRange(averageValues.Count - numErrorsToBacktrackOn, numErrorsToBacktrackOn);
                        errorCount = 0;

                        //rollback i
                        i -= numErrorsToBacktrackOn;

                        var errSamples = series.Where((x, idx) => idx > i && idx <= numErrorsToBacktrackOn + i);
                        //reset values
                        average = CalcAverage(errSamples);
                        sumOfSquares = CalcSumOfSquares(errSamples, average);

                        continue;
                    }
                }
                //normal values
                else
                {
                    errorCount = 0;
                    averageValues.Add(average);
                    alpha = algAlpha;
                }
                average = alpha * sample + (1 - alpha) * average;
                sumOfSquares = alpha * Math.Pow(sample, 2) + (1 - alpha) * sumOfSquares;
            }
            return averageValues;
        }


        //Note: Stddev[t] = sqrt(sumOfSquares[t] – average[t]^2)
        private double CalcStddev(double sumOfSquares, double average)
        {
            return Math.Sqrt(sumOfSquares - Math.Pow(average, 2));
        }

    }
}
