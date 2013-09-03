using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Microsoft.Research.DynamicDataDisplay.DataSources;
using System.Windows;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Research.DynamicDataDisplay.PointMarkers;

namespace SyncSubsByComparison
{
    //TODO: create a batch mode and a commandline mode.
    //TODO: make it easier to sync by auto downloading english sub, and auto finding lang sub when given a video filename.

    public class MainVM : INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;

        private bool _removeAbnormalPoints = true;
        private BingTranslator _bingTranslator = null;
        private ObservableDataSource<Point> _actualData = new ObservableDataSource<Point>();
        private ObservableDataSource<Point> _baselineData = new ObservableDataSource<Point>();
        private ObservableDataSource<Point> _regressionData = new ObservableDataSource<Point>();
        private ObservableDataSource<Point> _stepsData = new ObservableDataSource<Point>();
        private double _orderedLetterSimilarityTreshold = 0.65;
        private int _linesToSearchForward = 18;
        private int _minimumLettersForMatch = 9;
        private string _translationText;
        private double _alpha = 0.35d;
        private double _startSectionLength = 6;
        //private string _languageSrt = @"c:\TEST\BG - 3x20\heb.srt";
        //private string _timingSrt = @"c:\TEST\BG - 3x20\synced.srt";
        private string _languageSrt = @"c:\TEST\Arrow 20\heb.srt";
        private string _timingSrt = @"c:\TEST\Arrow 20\synced.srt";

        private double _normalZoneAmplitude = 3;
        private double _timeStampDurationMultiplyer = 1.0d;
        private string _countMatchPoints = "0";
        private LineTypes _selectedLineType = LineTypes.OriginalMatch;
        private string _selectedEncodingName = "Hebrew (Windows)";
        private SampleCollection _lnBaseline = null;
        private SampleCollection _lnOriginal = null;
        private SampleCollection _lnRegression = null;
        private SampleCollection _lnSteps = null;
        private SubtitleInfo _langSub;
        private SubtitleInfo _timingSub;

        #region properties
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

        public LineTypes SelectedLineType
        {
            get { return _selectedLineType; }
            set
            {
                _selectedLineType = value;


                if (PropertyChanged != null)
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("SelectedLineType"));
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

        public string SelectedEncodingName
        {
            get { return _selectedEncodingName; }
            set
            {
                _selectedEncodingName = value;
                if (PropertyChanged != null)
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("SelectedEncodingName"));
            }
        }

        public Encoding SelectedEncoding
        {
            get { return Encoding.GetEncodings().Where(e => e.DisplayName == _selectedEncodingName).First().GetEncoding(); }
        }

        public IEnumerable<string> AllEncodings
        {
            get { return Encoding.GetEncodings().Select(e => e.DisplayName).OrderBy(e => e); }
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

        public ObservableDataSource<Point> RegressionData
        {
            get { return _regressionData; }
            set
            {
                _regressionData = value;
                if (PropertyChanged != null)
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("RegressionData"));
            }
        }

        public ObservableDataSource<Point> StepsData
        {
            get { return _stepsData; }
            set
            {
                _stepsData = value;
                if (PropertyChanged != null)
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs("StepsData"));
            }
        }

        public SubtitleInfo FixedSub
        {
            get { return SelectedLineForSubtitleFix != null ? SelectedLineForSubtitleFix.CreateFixedSubtitle(_langSub) : null; }
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

        public SampleCollection SelectedLineForSubtitleFix
        {
            get
            {
                //get selected line for subtitle
                switch (SelectedLineType)
                {
                    case LineTypes.Baseline:
                        return _lnBaseline;
                    case LineTypes.LinearRegression:
                        return _lnRegression;
                    case LineTypes.OriginalMatch:
                        return _lnOriginal;
                    case LineTypes.StepsClustering:
                        return _lnSteps;

                }
                return null;
            }
        }

        #endregion

        public MainVM()
        {
            _actualData.SetXYMapping(p => p);
            _baselineData.SetXYMapping(p => p);
            _regressionData.SetXYMapping(p => p);
            _stepsData.SetXYMapping(p => p);
        }

        #region private functions
        //private SubtitleInfo GetFixedSubtitle(SubtitleInfo subtitlToFix, List<KeyValuePair<LineInfo, LineInfo>> orderedMatchPoints, List<double> matchPointsDiffsFromGoodSync)
        //{
        //    var subtitle = subtitlToFix.CloneSub();
        //    int idx = 0;
        //    //attach average to actual timestamps
        //    foreach (var item in orderedMatchPoints)
        //    {
        //        item.Key.TimeStamp.IsOffsetCorrected = true;
        //        item.Key.TimeStamp.Correction = (long)matchPointsDiffsFromGoodSync[idx];
        //        ++idx;
        //    }

        //    //spread correction to all timestamps (including the ones not attached)
        //    long prevOffset = (long)matchPointsDiffsFromGoodSync[0];
        //    TimeStamp prev = null;

        //    foreach (var time in subtitle.TimeMarkers)
        //    {
        //        if (!time.IsOffsetCorrected)
        //        {
        //            var next = subtitle.Lines.Where(p => p.TimeStamp.FromTime > time.FromTime).FirstOrDefault(x => x.TimeStamp.IsOffsetCorrected);
        //            var currTime = time;
        //            var prevTime = prev;
        //            double newOffset = prevOffset;

        //            if (prevTime != null && next != null)
        //            {
        //                var nextTime = next.TimeStamp;

        //                //timeAfterPrev / timeInterval (=next-prev) = the precentage of movement in the X axis between the two points
        //                double part = ((double)currTime.FromTime - (double)prevTime.FromTime) / ((double)nextTime.FromTime - (double)prevTime.FromTime);

        //                //(change in corrections between prev -> next) * (calculated place between them =part) + (the base correction of prev =the stating point of this correction)
        //                newOffset = (((double)nextTime.Correction - (double)prev.Correction) * part) + (double)prevTime.Correction;
        //            }

        //            time.IsOffsetCorrected = true;
        //            time.Correction = (long)newOffset;
        //        }
        //        else
        //        {
        //            prevOffset = time.Correction;
        //            prev = time;
        //        }

        //        //extend duration for all subs
        //        time.Duration = (long)(TimeStampDurationMultiplyer * (double)time.Duration);
        //    }
        //    return subtitle;
        //}

        private List<KeyValuePair<LineInfo, LineInfo>> CalculateDiffAndBaseline(List<KeyValuePair<LineInfo, LineInfo>> ordered, out List<long> dataset2, out List<double> averages)
        {
            dataset2 = ordered.Select(x => x.Value.TimeStamp.FromTime - x.Key.TimeStamp.FromTime).ToList();

            var baseline = Baseline.CreateBaseline(dataset2, 7, (int)StartSectionLength, BaselineAlgAlpha, NormalZoneAmplitude);
            averages = baseline.Averages;

            //fix collections to remove abnormal values in preperation for using them in the sync later
            if (RemoveAbnormalPoints)
            {
                averages = averages.Where((p, i) => (!baseline.AbnormalPoints.Contains(i))).ToList();
                ordered = ordered.Where((p, i) => (!baseline.AbnormalPoints.Contains(i))).ToList();
                dataset2 = dataset2.Where((p, i) => (!baseline.AbnormalPoints.Contains(i))).ToList();
                baseline = Baseline.CreateBaseline(dataset2, 7, (int)StartSectionLength, BaselineAlgAlpha, 3);
                averages = baseline.Averages;
            }

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
                if (_bingTranslator == null)
                    _bingTranslator = new BingTranslator();

                translator = _bingTranslator;
            }

            langSub = new SubtitleInfo(translator);
            timingSub = new SubtitleInfo(translator);

            langSub.LoadSrtFile(LanguageSrtFile, SelectedEncoding);
            timingSub.LoadSrtFile(TimingSrtFile, Encoding.ASCII);

            try
            {
                langSub.Translate();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Translation error: " + ex.Message);
            }

            TranslationText = langSub.GetTranslatedSrtString();
            if (!transFileExists)
                File.WriteAllText(LanguageSrtFile + ".trans", TranslationText);
        }

        private void UpdateGraph(SampleCollection col, ObservableDataSource<Point> dataSourceToUpdate)
        {
            var points = col.Select(p => new Point(p.X, p.Y));

            dataSourceToUpdate.Collection.Clear();
            dataSourceToUpdate.AppendMany(points);
        }

        private Dictionary<LineInfo, LineInfo> FindBestMatch(SubtitleInfo langSub, SubtitleInfo timingSub, int matchLinesToSearchForward, int matchMinimumLettersForMatch, double matchSimilarityThreshold)
        {
            Dictionary<LineInfo, LineInfo> matchedLangLines2timingLines1 = FindMatch(langSub, timingSub, matchLinesToSearchForward, matchMinimumLettersForMatch, matchSimilarityThreshold);
            Dictionary<LineInfo, LineInfo> matchedLangLines2timingLines2 = ReverseKeyVal(FindMatch(timingSub, langSub, matchLinesToSearchForward, matchMinimumLettersForMatch, matchSimilarityThreshold));
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

        private Dictionary<LineInfo, LineInfo> FindMatch(SubtitleInfo langSub, SubtitleInfo timingSub, int matchLinesToSearchForward, int matchMinimumLettersForMatch, double matchSimilarityThreshold)
        {
            Dictionary<LineInfo, LineInfo> matchedLangLines2timingLines = new Dictionary<LineInfo, LineInfo>();
            int timingSrtPos = 0;

            //start a bit after the beginning to skip unwanted chatter lines
            for (int i = 0; i < langSub.Lines.Count(); ++i)
            {
                var sline = langSub.Lines[i];
                int endSearch = Math.Min(timingSub.Lines.Count(), timingSrtPos + matchLinesToSearchForward);
                for (int j = timingSrtPos; j < endSearch; ++j)
                {
                    var tline = timingSub.Lines[j];

                    //if we have a match
                    if (tline.CalcIsWordMatch(sline, matchMinimumLettersForMatch, matchSimilarityThreshold))
                    {
                        if (!matchedLangLines2timingLines.ContainsKey(sline))
                            matchedLangLines2timingLines.Add(sline, tline);
                        timingSrtPos = j + 1;
                    }
                }
            }
            return matchedLangLines2timingLines;
        }
        #endregion private functions

        internal void AutoSyncSubtitles()
        {
            SubtitleInfo langSub;
            SubtitleInfo timingSub;
            object locker = new object();
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
                Parallel.For(10, 19, j =>
                {
                    {
                        Dictionary<LineInfo, LineInfo> matchedLangLines2timingLines = FindBestMatch(langSub, timingSub, j, i, MatchSimilarityThreshold);
                        int numMatches = matchedLangLines2timingLines.Count();
                        lock (locker)
                        {
                            if (numMatches > maxMatchLines)
                            {
                                maxMatchLines = numMatches;
                                bestMatchedLines = matchedLangLines2timingLines;
                                bestMatchMinimumLettersForMatch = i;
                                bestMatchLinesToSearchForward = j;
                            }
                        }
                    }
                });
            }
            MatchLinesToSearchForward = bestMatchLinesToSearchForward;
            MatchMinimumLettersForMatch = bestMatchMinimumLettersForMatch;

            SyncSubtitles();
        }

        internal void SyncSubtitles()
        {
            try
            {

                PrepareInputSubs(out _langSub, out _timingSub);
            }
            catch
            {
                return;
            }

            Dictionary<LineInfo, LineInfo> matchedLangLines2timingLines = FindBestMatch(_langSub, _timingSub, MatchLinesToSearchForward, MatchMinimumLettersForMatch, MatchSimilarityThreshold);

            //update the counter.
            CountMatchPoints = matchedLangLines2timingLines.Count() + " of " + _langSub.Lines.Count();

            var orderedMatchPoints = matchedLangLines2timingLines.OrderBy(x => x.Key.TimeStamp.FromTime).ToList();

            var diffPoints = orderedMatchPoints.Select(x => (double)(x.Value.TimeStamp.FromTime - x.Key.TimeStamp.FromTime)).ToList();

            ////correct for line in sub diff
            //for (int i = 0; i < orderedMatchPoints.Count(); ++i)
            //{
            //    var keyval = orderedMatchPoints[i];
            //    var langLine = keyval.Key;
            //    var syncLine = keyval.Value;
            //    int shift = langLine.LineNumberInSub - syncLine.LineNumberInSub;
            //    if (shift != 0)
            //    {
            //        double timeForOneLine = (double)syncLine.TimeStamp.Duration / (double)syncLine.TimeStamp.Lines.Count();
            //        double shiftCorrection = shift * timeForOneLine;
            //        diffPoints[i] -= shiftCorrection;
            //    }
            //}

            var timesForXAxis = orderedMatchPoints.Select(p => p.Key.TimeStamp.FromTime).ToList();
            var mypoints = diffPoints.Select((p, i) => new MyPoint(timesForXAxis[i], p));

            _lnOriginal = new SampleCollection(mypoints);

            Dictionary<double, string> descsByX = new Dictionary<double, string>();

            //get desc for each x
            var listOfDescs = orderedMatchPoints.Select(m => new { x = m.Key.TimeStamp.FromTime, desc = m.Key.Line + "\n" + m.Value.Line }).ToList();
            listOfDescs.ForEach(p =>
            {
                if (!descsByX.ContainsKey(p.x))
                    descsByX.Add(p.x, p.desc);
            });
            _lnOriginal.PointDescByXValue = descsByX;

            if (RemoveAbnormalPoints)
            {
                _lnOriginal = _lnOriginal.FilterAbnormalsByBaseline(BaselineAlgAlpha, NormalZoneAmplitude, (int)StartSectionLength);
            }

            _lnBaseline = _lnOriginal.GetBaseline(BaselineAlgAlpha, NormalZoneAmplitude, (int)StartSectionLength);
            _lnRegression = _lnOriginal.GetLinearRegression();

            _lnSteps = _lnOriginal.GetStepLineByKMeans(6);

            //update the graphs
            UpdateGraph(_lnBaseline, _baselineData);
            UpdateGraph(_lnRegression, _regressionData);
            UpdateGraph(_lnSteps, _stepsData);
            UpdateGraph(_lnOriginal, _actualData);
        }

        internal void EditTargetGraph(Point newPoint, ObservableDataSource<Point> dataSourceToUpdate)
        {
            var pt = SelectedLineForSubtitleFix.Where(s => newPoint.X == s.X).First();
            pt.Y = newPoint.Y;
            UpdateGraph(SelectedLineForSubtitleFix, dataSourceToUpdate);
        }

        internal void UpdateEditableLine(int _selectedPointIndex, double x, double y)
        {
            _lnOriginal[_selectedPointIndex].Y = y;
        }

        internal string GetTextForPoint(Point pt)
        {
            string desc = _lnOriginal.PointDescByXValue[pt.X];
            string retStr = "(" + TimeSpan.FromMilliseconds(pt.X).ToString(@"hh\:mm\:ss\.ff") + ", correction= " + pt.Y.ToString("0.0") + "):\n" + desc;
            return retStr;
        }

    }
}
