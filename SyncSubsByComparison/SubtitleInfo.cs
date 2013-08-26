using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading;

namespace SyncSubsByComparison
{
    public class SubtitleInfo
    {

        List<LineInfo> _listOfLines = new List<LineInfo>();
        List<TimeStamp> _listOfTimeMarkers = new List<TimeStamp>();
        static Regex _regTimeStamp = new Regex(@"\d+?\w*?\s+(?<fromTime>\d\d:\d\d:\d\d,\d\d\d)\s+.+?>\s+(?<toTime>\d\d:\d\d:\d\d,\d\d\d)(?<lines>.*?)(?=\d+?\w*?\s+\d\d:\d\d:\d\d,\d\d\d\s+.+?>|\z)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

        public List<LineInfo> Lines
        {
            get { return _listOfLines; }
            set { _listOfLines = value; }
        }

        public List<TimeStamp> TimeMarkers
        {
            get { return _listOfTimeMarkers; }
            set { _listOfTimeMarkers = value; }
        }
        ITranslator _translator = null;
        public SubtitleInfo(ITranslator trans)
        {
            _translator = trans;
        }
        public string DetectLanguage()
        {

            string allLines = Lines.Select(a => a.Line).Aggregate((a, b) => a + "\n" + b);
            string lang = _translator.DetectLanguage(allLines.Substring(0, 100));
            return lang;
        }

        string _language = null;
        string SubtitleLanguage
        {
            get
            {

                if (_language == null)
                {
                    if (Lines.Count() == 0)
                        return null;

                    _language = DetectLanguage();
                }
                return _language;
            }
        }

        public SubtitleInfo CloneSub()
        {
            SubtitleInfo clone = new SubtitleInfo(this._translator);
            clone._language = this._language;
            clone._listOfLines = new List<LineInfo>();
            clone._listOfTimeMarkers = new List<TimeStamp>();
            foreach (var ts in _listOfTimeMarkers)
            {
                TimeStamp tsn = new TimeStamp() { Correction = ts.Correction, Duration = ts.Duration, FromTime = ts.FromTime };
                foreach (var line in ts.Lines)
                {
                    var nline = new LineInfo() { Line = line.Line, TimeStamp = tsn, TranslatedLine = line.TranslatedLine };
                    tsn.Lines.Add(nline);
                    clone._listOfLines.Add(nline);
                }
                clone._listOfTimeMarkers.Add(tsn);
            }
            return clone;
        }


        public void Translate()
        {
            if (SubtitleLanguage == "en")
                return;


            IEnumerable<string> allLines = Lines.Select(a => a.Line);

            var translatedLines = _translator.TranslateLines(allLines, "en", SubtitleLanguage).ToList();

            for (int i = 0; i < allLines.Count(); ++i)
            {
                Lines[i].TranslatedLine = translatedLines[i];
            }
        }


        public void LoadSrtFile(string file, Encoding enc)
        {
            string text = File.ReadAllText(file, enc);
            LoadSrtFileContent(text);
        }

        public void LoadSrtFileContent(string content)
        {
            var matches = _regTimeStamp.Matches(content);
            if (matches.Count == 0)
                return;

            foreach (Match m in matches)
            {
                var from = ParseDateToMiliseconds(m.Groups["fromTime"].Value);
                var to = ParseDateToMiliseconds(m.Groups["toTime"].Value);

                TimeStamp currentTimeStamp = new TimeStamp() { FromTime = from, Duration = to - from };
                currentTimeStamp.Lines.AddRange(m.Groups["lines"].Value.Split("\n\r".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Select(l => new LineInfo() { Line = l, TimeStamp = currentTimeStamp}));
                _listOfTimeMarkers.Add(currentTimeStamp);
                _listOfLines.AddRange(currentTimeStamp.Lines);
            }

        }

        private static long ParseDateToMiliseconds(string str)
        {
            var time = DateTime.ParseExact(str, "HH:mm:ss,fff", Thread.CurrentThread.CurrentCulture);
            return time.Millisecond + time.Minute * 60 * 1000 + time.Second * 1000 + time.Hour * 60 * 60 * 1000;
        }

        internal string GetTranslatedSrtString()
        {
            StringBuilder sb = new StringBuilder();
            int globalLineCounter = 1;
            foreach (var time in TimeMarkers)
            {
                sb.AppendLine(globalLineCounter.ToString());
                string strFromTime = TimespanToString(TimeSpan.FromMilliseconds(time.FromTime + time.Correction));//.ToString("hh\\:mm\\:ss\\,fff");
                string strToTime = TimespanToString(TimeSpan.FromMilliseconds(time.FromTime + time.Correction + time.Duration));//.ToString("hh\\:mm\\:ss\\,fff");
                sb.AppendLine(strFromTime + " --> " + strToTime);
                foreach (var lineinf in time.Lines)
                {
                    sb.AppendLine(lineinf.TranslatedLine);
                }

                ++globalLineCounter;
            }
            return sb.ToString();
        }

        internal void WriteSrt(string p, Encoding chosenEncoding)
        {
            /*
1
00:00:01,000 --> 00:00:04,074
Subtitles downloaded from www.OpenSubtitles.org

2
00:00:18,977 --> 00:00:20,728
(BOY COUGHlNG)

3
00:00:26,484 --> 00:00:29,278
(TAKE ME OUT TO THE BALL GAME
PLAYlNG ON VlDEO GAME)
             */


            StringBuilder sb = new StringBuilder();
            int globalLineCounter = 1;
            for (int i = 0; i < TimeMarkers.Count(); ++i)
            {
                var time = TimeMarkers[i];
                sb.AppendLine(globalLineCounter.ToString());
                string strFromTime = TimespanToString(TimeSpan.FromMilliseconds(time.FromTime + time.Correction));//.ToString("hh\\:mm\\:ss\\,fff");
                var ToTime = TimeSpan.FromMilliseconds(time.FromTime + time.Correction + time.Duration);

                //truncate duration so it will not overlap next sub
                if (i < (TimeMarkers.Count() - 1))
                {
                    var ntime = TimeMarkers[i + 1];
                    var nToTime = TimeSpan.FromMilliseconds(ntime.FromTime + ntime.Correction);
                    ToTime = (ToTime <= nToTime) ? ToTime : nToTime - TimeSpan.FromMilliseconds(100);
                }

                string strToTime = TimespanToString(ToTime);

                sb.AppendLine(strFromTime + " --> " + strToTime);

                foreach (var lineinf in time.Lines)
                {
                    sb.AppendLine(lineinf.Line);
                }
                sb.AppendLine();

                ++globalLineCounter;
            }
            File.WriteAllText(p, sb.ToString(), chosenEncoding);
        }

        private string TimespanToString(TimeSpan timeSpan)
        {
            return timeSpan.Hours.ToString("00") + ":" + timeSpan.Minutes.ToString("00") + ":" + timeSpan.Seconds.ToString("00") + "," + timeSpan.Milliseconds.ToString("000");
        }
    }
}
