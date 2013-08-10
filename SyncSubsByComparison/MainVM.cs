using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Microsoft.Research.DynamicDataDisplay.DataSources;
using System.Windows;
using System.IO;

namespace SyncSubsByComparison
{
    public class MainVM : INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;

        private bool _syncAccordingToMatch;
        private bool _removeAbnormalPoints;
        private BingTranslator _bingTranslator = new BingTranslator();
        private SubtitleInfo _fixedSub;
        private ObservableDataSource<Point> _actualData = new ObservableDataSource<Point>();
        private ObservableDataSource<Point> _baselineData = new ObservableDataSource<Point>();
        private double _orderedLetterSimilarityTreshold = 0.65;
        private int _linesToSearchForward = 18;
        private int _minimumLettersForMatch = 9;
        private string _translationText;
        private double _alpha = 0.30d;
        private double _startSectionLength = 10;
        //private string _languageSrt = @"c:\TEST\Battlestar.Galactica.S03E10.The.Passage.WS.DSR.XviD-ORENJi.srt";
        //private string _timingSrt = @"c:\TEST\battlestar_galactica.3x10.the_passage.dvdrip_xvid-fov.srt";
        private string _languageSrt = @"c:\TEST\BG - 3x20\heb.srt";
        private string _timingSrt = @"c:\TEST\BG - 3x20\synced.srt";
        private double _normalZoneAmplitude = 3;
        private double _timeStampDurationMultiplyer = 1.0d;

        private string _countMatchPoints = "0";

        public string CountMatchPoints
        {
            get { return _countMatchPoints; }
            set
            {
                _countMatchPoints = value;
                if (PropertyChanged != null)
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("CountMatchPoints"));
            }
        }

        public int MatchMinimumLettersForMatch
        {
            get { return _minimumLettersForMatch; }
            set
            {
                _minimumLettersForMatch = value;
                if (PropertyChanged != null)
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("MatchMinimumLettersForMatch"));
            }
        }

        public int MatchLinesToSearchForward
        {
            get { return _linesToSearchForward; }
            set
            {
                _linesToSearchForward = value;
                if (PropertyChanged != null)
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("MatchLinesToSearchForward"));
            }
        }


        public double MatchSimilarityThreshold
        {
            get { return _orderedLetterSimilarityTreshold; }
            set
            {
                _orderedLetterSimilarityTreshold = value;
                if (PropertyChanged != null)
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("MatchSimilarityThreshold"));
            }
        }

        public double TimeStampDurationMultiplyer
        {
            get { return _timeStampDurationMultiplyer; }
            set
            {
                _timeStampDurationMultiplyer = value;
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs("TimeStampDurationMultiplyer"));
            }
        }

        public double NormalZoneAmplitude
        {
            get { return _normalZoneAmplitude; }
            set
            {
                _normalZoneAmplitude = value;
                if (PropertyChanged != null)
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("NormalZoneAmplitude"));
            }
        }

        public double BaselineAlgAlpha
        {
            get { return _alpha; }
            set
            {
                _alpha = value;
                if (PropertyChanged != null)
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("BaselineAlgAlpha"));
            }
        }

        public double StartSectionLength
        {
            get { return _startSectionLength; }
            set
            {
                _startSectionLength = value;
                if (PropertyChanged != null)
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("StartSectionLength"));
            }
        }

        public string LanguageSrtFile
        {
            get { return _languageSrt; }
            set
            {
                _languageSrt = value;
                if (PropertyChanged != null)
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("LanguageSrtFile"));
            }
        }

        public bool SyncAccordingToMatch
        {
            get { return _syncAccordingToMatch; }
            set
            {
                _syncAccordingToMatch = value;
                RemoveAbnormalPoints = value;
                if (PropertyChanged != null)
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("SyncAccordingToMatch"));
            }
        }

        public string TimingSrtFile
        {
            get { return _timingSrt; }
            set
            {
                _timingSrt = value;
                if (PropertyChanged != null)
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("TimingSrtFile"));
            }
        }

        public ObservableDataSource<Point> ActualData
        {
            get { return _actualData; }
            set
            {
                _actualData = value;
                if (PropertyChanged != null)
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("ActualData"));
            }
        }

        public ObservableDataSource<Point> BaselineData
        {
            get { return _baselineData; }
            set
            {
                _baselineData = value;
                if (PropertyChanged != null)
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("BaselineData"));
            }
        }

        public SubtitleInfo FixedSub
        {
            get { return _fixedSub; }
            set
            {
                _fixedSub = value;
                if (PropertyChanged != null)
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("FixedSub"));
            }
        }

        public string TranslationText
        {
            get { return _translationText; }
            set
            {
                _translationText = value;
                if (PropertyChanged != null)
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("TranslationText"));
            }
        }

        public bool RemoveAbnormalPoints
        {
            get { return _removeAbnormalPoints; }
            set
            {
                _removeAbnormalPoints = value;
                if (PropertyChanged != null)
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("RemoveAbnormalPoints"));
            }
        }

        public MainVM()
        {
            _actualData.SetXYMapping(p => p);
            _baselineData.SetXYMapping(p => p);
        }

        public void AutoSyncSubtitles()
        {
            SubtitleInfo langSub;
            SubtitleInfo timingSub;
            try
            {
                PrepareInputSubs(out langSub, out timingSub);
            }
            catch
            {
                return;
            }

            int bestMatchMinimumLettersForMatch = 0;
            int bestMatchLinesToSearchForward = 0;
            int maxMatchLines = 0;
            Dictionary<LineInfo, LineInfo> bestMatchedLines = null;
            for (int i = 6; i <= 7; ++i)
            {
                for (int j = 10; j <= 19; ++j)
                {
                    MatchMinimumLettersForMatch = i;
                    MatchLinesToSearchForward = j;

                    Dictionary<LineInfo, LineInfo> matchedLangLines2timingLines = FindBestMatch(langSub, timingSub);
                    int numMatches = matchedLangLines2timingLines.Count();
                    if (numMatches > maxMatchLines)
                    {
                        maxMatchLines = numMatches;
                        bestMatchedLines = matchedLangLines2timingLines;
                        bestMatchMinimumLettersForMatch = MatchMinimumLettersForMatch;
                        bestMatchLinesToSearchForward = MatchLinesToSearchForward;
                    }
                }
            }
            MatchLinesToSearchForward = bestMatchLinesToSearchForward;
            MatchMinimumLettersForMatch = bestMatchMinimumLettersForMatch;

            //update the counter.
            CountMatchPoints = bestMatchedLines.Count + " of " + langSub.Lines.Count();

            var orderedMatchPoints = bestMatchedLines.OrderBy(x => x.Key.TimeStamp.FromTime).ToList();
            List<long> diffs;
            List<double> averages;
            IEnumerable<long> timesForXAxis;

            //BaselineAlgAlpha = 0.3;
            //BaselineAlgAlpha += 0.05;

            orderedMatchPoints = CalculateDiffAndBaseline(orderedMatchPoints, out diffs, out averages);

            timesForXAxis = orderedMatchPoints.Select(p => p.Key.TimeStamp.FromTime);
            UpdateGraph(timesForXAxis, averages, _baselineData);
            UpdateGraph(timesForXAxis, diffs.Select(p => (double)p), _actualData);

            //TODO: move to save button
            _fixedSub = GetFixedSubtitle(langSub, orderedMatchPoints, averages);
        }

        public void SyncSubtitles()
        {
            SubtitleInfo langSub;
            SubtitleInfo timingSub;
            try
            {

                PrepareInputSubs(out langSub, out timingSub);
            }
            catch
            {
                return;
            }

            Dictionary<LineInfo, LineInfo> matchedLangLines2timingLines = FindBestMatch(langSub, timingSub);

            //update the counter.
            CountMatchPoints = matchedLangLines2timingLines.Count() + " of " + langSub.Lines.Count();

            var orderedMatchPoints = matchedLangLines2timingLines.OrderBy(x => x.Key.TimeStamp.FromTime).ToList();
            List<long> diffs;
            List<double> averages;
            IEnumerable<long> timesForXAxis;
            orderedMatchPoints = CalculateDiffAndBaseline(orderedMatchPoints, out diffs, out averages);

            timesForXAxis = orderedMatchPoints.Select(p => p.Key.TimeStamp.FromTime);
            UpdateGraph(timesForXAxis, averages, _baselineData);
            UpdateGraph(timesForXAxis, diffs.Select(p => (double)p), _actualData);

            //TODO: move to save button
            _fixedSub = GetFixedSubtitle(langSub, orderedMatchPoints, averages);
        }

        private SubtitleInfo GetFixedSubtitle(SubtitleInfo subtitlToFix, List<KeyValuePair<LineInfo, LineInfo>> orderedMatchPoints, List<double> matchPointsDiffsFromGoodSync)
        {
            var subtitle = subtitlToFix.CloneSub();
            int idx = 0;
            //attach average to actual timestamps
            foreach (var item in orderedMatchPoints)
            {
                item.Key.TimeStamp.IsOffsetCorrected = true;
                item.Key.TimeStamp.Correction = (long)matchPointsDiffsFromGoodSync[idx];
                ++idx;
            }

            //spread correction to all timestamps (including the ones not attached)
            long prevOffset = (long)matchPointsDiffsFromGoodSync[0];
            TimeStamp prev = null;

            foreach (var time in subtitle.TimeMarkers)
            {
                if (!time.IsOffsetCorrected)
                {
                    var next = subtitle.Lines.Where(p => p.TimeStamp.FromTime > time.FromTime).FirstOrDefault(x => x.TimeStamp.IsOffsetCorrected);
                    var currTime = time;
                    var prevTime = prev;
                    double newOffset = prevOffset;

                    if (prevTime != null && next != null)
                    {
                        var nextTime = next.TimeStamp;

                        //timeAfterPrev / timeInterval (=next-prev) = the precentage of movement in the X axis between the two points
                        double part = ((double)currTime.FromTime - (double)prevTime.FromTime) / ((double)nextTime.FromTime - (double)prevTime.FromTime);

                        //(change in corrections between prev -> next) * (calculated place between them =part) + (the base correction of prev =the stating point of this correction)
                        newOffset = (((double)nextTime.Correction - (double)prev.Correction) * part) + (double)prevTime.Correction;
                    }

                    time.IsOffsetCorrected = true;
                    time.Correction = (long)newOffset;
                }
                else
                {
                    prevOffset = time.Correction;
                    prev = time;
                }

                //extend duration for all subs
                time.Duration = (long)(TimeStampDurationMultiplyer * (double)time.Duration);
            }
            return subtitle;
        }

        private List<KeyValuePair<LineInfo, LineInfo>> CalculateDiffAndBaseline(List<KeyValuePair<LineInfo, LineInfo>> ordered, out List<long> dataset2, out List<double> averages)
        {
            var lastTimeStamForSync = ordered.Last().Value.TimeStamp.FromTime;
            dataset2 = ordered.Select(x => x.Value.TimeStamp.FromTime - x.Key.TimeStamp.FromTime).ToList();

            var baseline = CreateBaseline(dataset2, 7, (int)StartSectionLength, BaselineAlgAlpha, NormalZoneAmplitude);
            averages = baseline.Averages;
            //List<KeyValuePair<LineInfo, LineInfo> ordered1;
            //fix collections to remove abnormal values in preperation for using them in the sync later
            if (RemoveAbnormalPoints)
            {
                averages = averages.Where((p, i) => (!baseline.AbnormalPoints.Contains(i))).ToList();
                ordered = ordered.Where((p, i) => (!baseline.AbnormalPoints.Contains(i))).ToList();
                dataset2 = dataset2.Where((p, i) => (!baseline.AbnormalPoints.Contains(i))).ToList();
                baseline = CreateBaseline(dataset2, 7, (int)StartSectionLength, BaselineAlgAlpha, 3);
                averages = baseline.Averages;
            }

            if (SyncAccordingToMatch)
            {
                for (int i = 0; i < averages.Count(); ++i)
                    averages[i] = ordered[i].Value.TimeStamp.FromTime - ordered[i].Key.TimeStamp.FromTime;
            }
            //var actualPoints = dataset2.Select((p, i) => new Point(i, p));
            //_actualData.Collection.Clear();
            //_actualData.AppendMany(actualPoints);

            //timesForXAxis = ordered.Select(p => p.Key.TimeStamp.FromTime);
            return ordered;
        }

        private void PrepareInputSubs(out SubtitleInfo langSub, out SubtitleInfo timingSub)
        {
            ITranslator translator;

            string translationFileName = LanguageSrtFile + ".trans";
            bool transFileExists = (File.Exists(translationFileName));

            if (!string.IsNullOrWhiteSpace(_translationText) || transFileExists)
            {
                if (transFileExists)
                {
                    _translationText = File.ReadAllText(translationFileName);
                }
                translator = new SrtParserTranslator(_translationText);
            }
            else
            {
                translator = _bingTranslator;
            }

            langSub = new SubtitleInfo(translator);
            timingSub = new SubtitleInfo(translator);

            langSub.LoadSrtFile(LanguageSrtFile);
            timingSub.LoadSrtFile(TimingSrtFile);

            //langSub.Translate(Google.API.Translate.Language.English);
            //timingSub.Translate(Google.API.Translate.Language.English);
            try
            {
                langSub.Translate();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Translation error: " + ex.Message);
                //return;
            }


            //timingSub.Translate();
            TranslationText = langSub.GetTranslatedSrtString();
            if (!transFileExists)
                File.WriteAllText(LanguageSrtFile + ".trans", TranslationText);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="series"></param>
        /// <param name="numErrorsToBacktrackOn"></param>
        /// <param name="initialSectionLength"></param>
        /// <param name="algAlpha">the forgetfullness constant, controls the impact of every new sample on the average and deviation, as well as the (much smaller) impact of abnormal samples (should be between 0.01 and 0.05)</param>
        /// <param name="normalcyAmplitudeConstant">the number of standart deviations from the average line in which a sample is considered ok to include in calculation, this creates a zone surrounding the average from above and below which is the allowed zone for samples, should usually be 3</param>
        /// <returns></returns>
        public Baseline CreateBaseline(List<long> series, int numErrorsToBacktrackOn, int initialSectionLength, double algAlpha = 0.02d, double normalcyAmplitudeConstant = 3)
        {
            List<double> averageValues = new List<double>();
            var leadIn = series.Take(initialSectionLength);
            double average = CalcAverage(leadIn);
            double sumOfSquares = CalcSumOfSquares(leadIn, average);
            for (int i = 0; i < initialSectionLength; i++)
                averageValues.Add(average);

            //algAlpha = 0.02d;
            double alpha = algAlpha;
            int errorCount = 0;

            List<long> abnormalValuePositions = new List<long>();

            for (int i = initialSectionLength; i < series.Count(); ++i)
            {
                var sample = series[i];
                var stdDeviation = CalcStddev(sumOfSquares, average);
                double distanceFromAvg = Math.Abs(sample - average);

                //an abnormal value is found
                if (distanceFromAvg > normalcyAmplitudeConstant * stdDeviation)
                {
                    alpha = Math.Pow(algAlpha, (distanceFromAvg / (stdDeviation)) - (normalcyAmplitudeConstant - 1));

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
                        i -= (numErrorsToBacktrackOn + 1);

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
                    alpha = algAlpha;
                }
                averageValues.Add(average);

                average = alpha * sample + (1 - alpha) * average;
                sumOfSquares = alpha * Math.Pow(sample, 2) + (1 - alpha) * sumOfSquares;
            }
            var abnormalPointsHash = new HashSet<long>();
            abnormalValuePositions.ForEach(a => abnormalPointsHash.Add(a));
            return new Baseline() { AbnormalPoints = abnormalPointsHash, Averages = averageValues };
        }

        private void UpdateGraph(IEnumerable<long> xAxis, IEnumerable<double> values, ObservableDataSource<Point> dataSourceToUpdate)
        {
            //var points = values.Select((p, i) => new Point(i, (double)p));
            var points = values.Select((p, i) => new Point(xAxis.ElementAt(i), (double)p));

            dataSourceToUpdate.Collection.Clear();
            dataSourceToUpdate.AppendMany(points);
        }

        private Dictionary<LineInfo, LineInfo> FindBestMatch(SubtitleInfo langSub, SubtitleInfo timingSub)
        {
            Dictionary<LineInfo, LineInfo> matchedLangLines2timingLines1 = FindMatch(langSub, timingSub);
            Dictionary<LineInfo, LineInfo> matchedLangLines2timingLines2 = ReverseKeyVal(FindMatch(timingSub, langSub));
            Dictionary<LineInfo, LineInfo> matchedLangLines2timingLines;

            if (matchedLangLines2timingLines1.Count() > matchedLangLines2timingLines2.Count())
                matchedLangLines2timingLines = matchedLangLines2timingLines1;
            else
                matchedLangLines2timingLines = matchedLangLines2timingLines2;
            return matchedLangLines2timingLines;
        }

        private Dictionary<LineInfo, LineInfo> ReverseKeyVal(Dictionary<LineInfo, LineInfo> dictionary)
        {
            Dictionary<LineInfo, LineInfo> reversed = new Dictionary<LineInfo, LineInfo>();
            foreach (var keyval in dictionary)
            {
                reversed.Add(keyval.Value, keyval.Key);
            }
            return reversed;
        }

        private Dictionary<LineInfo, LineInfo> FindMatch(SubtitleInfo langSub, SubtitleInfo timingSub)
        {
            Dictionary<LineInfo, LineInfo> matchedLangLines2timingLines = new Dictionary<LineInfo, LineInfo>();
            int timingSrtPos = 0;

            //start a bit after the beginning to skip unwanted chatter lines
            for (int i = 0; i < langSub.Lines.Count(); ++i)
            {
                var sline = langSub.Lines[i];
                int endSearch = Math.Min(timingSub.Lines.Count(), timingSrtPos + this.MatchLinesToSearchForward);
                for (int j = timingSrtPos; j < endSearch; ++j)
                {
                    var tline = timingSub.Lines[j];

                    //if we have a match
                    if (tline.CalcIsWordMatch(sline, MatchMinimumLettersForMatch, MatchSimilarityThreshold))
                    {
                        if (!matchedLangLines2timingLines.ContainsKey(sline))
                            matchedLangLines2timingLines.Add(sline, tline);
                        timingSrtPos = j + 1;
                    }
                }
            }
            return matchedLangLines2timingLines;
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


        ///Note: Stddev[t] = sqrt(sumOfSquares[t] – average[t]^2)
        private double CalcStddev(double sumOfSquares, double average)
        {
            return Math.Sqrt(sumOfSquares - Math.Pow(average, 2));
        }

    }
}
