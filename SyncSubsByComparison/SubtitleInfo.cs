using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading;
//using SyncSubsByComparison.TranslatorService;

namespace SyncSubsByComparison
{
    public class SubtitleInfo
    {
        List<LineInfo> _listOfLines = new List<LineInfo>();
        List<TimeStamp> _listOfTimeMarkers = new List<TimeStamp>();
        

        //00:00:26,484 --> 00:00:29,278
        //static Regex _regTimeStamp = new Regex(@"(?<fromTime>\d\d:\d\d:\d\d,\d\d\d)\s+.+?>\s+(?<toTime>\d\d:\d\d:\d\d,\d\d\d)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
        //static Regex _regNumbering = new Regex(@"\d+.*", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

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
        //Google.API.Translate.TranslateClient _translator = new TranslateClient("");
        //string bingTranslateAppID = "zRrUfDpO57x/9tnx16m/2oEpXzK6amgydzKi7tEBJXw";
        public string DetectLanguage()
        {
            //TranslatorService.LanguageServiceClient client = new TranslatorService.LanguageServiceClient();
            //BingTranslationAdapter adapter = new BingTranslationAdapter();

            string allLines = Lines.Select(a => a.Line).Aggregate((a, b) => a + "\n" + b);
            //string lang = "en";
string lang = _translator.DetectLanguage(allLines.Substring(0, 100));
            //return "he";
            //string lang = client.Detect(bingTranslateAppID, "asdad");//allLines.Substring(10)

            //bool isReliable = false;
            //double confidence;
            //GoogleLangaugeDetector g = new GoogleLangaugeDetector("asdas", VERSION.ONE_POINT_ZERO, "ABQIAAAAlNycYX5wojqaKddC8IIH5hQabF-q4UrefpY0of7YThOGLw-6IxTfQyShVwYA2-gXEaqXOKv2dzQwvw");
            //return (LANGUAGE)Enum.Parse(typeof(LANGUAGE), g.LanguageDetected);
            //client.Close();

return lang;

            //var lang = Translator.Detect("saasdas asdasdsa", out isReliable, out confidence);
            //return lang;
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
        
        

        public void Translate()
        {
            //BingTranslationAdapter adapter = new BingTranslationAdapter();
            //TranslatorService.LanguageServiceClient client = new TranslatorService.LanguageServiceClient();
            if (SubtitleLanguage == "en")
                return;


            IEnumerable< string> allLines = Lines.Select(a => a.Line);
            //.Aggregate((a, b) => a + "\n" + b)

            //string translation = "";
            //translation = adapter.TranslateArray(allLines, "en", SubtitleLanguage);

            //string[] translatedLines = translation.Split("\n\r".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            var translatedLines = _translator.TranslateLines(allLines, "en", SubtitleLanguage).ToList();
            //client.Close();

            for (int i = 0; i < allLines.Count(); ++i)
            {
                Lines[i].TranslatedLine = translatedLines[i];
            }
        }

        static Regex _regTimeStamp = new Regex(@"\d+?\w*?\s+(?<fromTime>\d\d:\d\d:\d\d,\d\d\d)\s+.+?>\s+(?<toTime>\d\d:\d\d:\d\d,\d\d\d)(?<lines>.*?)(?=\d+?\w*?\s+\d\d:\d\d:\d\d,\d\d\d\s+.+?>|\z)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
        //static Regex _regNumbering = new Regex(@"\d+.*", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

       

        public void LoadSrtFile(string file)
        {
             //List<string> _translation;
            string text = File.ReadAllText(file, Encoding.GetEncoding("Windows-1255"));
            //_translation = new List<string>();
            //_translation = translation.Split("\n\r".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Where(l => !_regTimeStamp.IsMatch(l));
            var matches = _regTimeStamp.Matches(text);
            if (matches.Count == 0)
                return;
            
            foreach (Match m in matches)
            {
                var from = ParseDateToMiliseconds(m.Groups["fromTime"].Value);
                var to = ParseDateToMiliseconds(m.Groups["toTime"].Value);

                TimeStamp currentTimeStamp = new TimeStamp() { FromTime = from, Duration = to - from };
                currentTimeStamp.Lines.AddRange(m.Groups["lines"].Value.Split("\n\r".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Select(l=>new LineInfo(){Line=l, TimeStamp=currentTimeStamp}));
                _listOfTimeMarkers.Add(currentTimeStamp);
                _listOfLines.AddRange(currentTimeStamp.Lines);
            }
        

            //List<SrtLine> listOfLines = new List<SrtLine>();
            //string[] lines = File.ReadAllLines(file, Encoding.GetEncoding("Windows-1255"));
            //TimeStamp currentTimeStamp = null;
            //int currentLineNumber = 0;
            //foreach (string line in lines)
            //{
            //    if (string.IsNullOrEmpty(line))
            //        continue;

            //    TimeStamp ts = TryReadTimeStamp(line);
            //    if (ts != null)
            //    {

            //        currentTimeStamp = ts;
            //        _listOfTimeMarkers.Add(ts);
            //        currentLineNumber = 0;
            //    }
            //    else if (CheckIsNumberingLine(line))
            //    {
            //        continue;
            //    }
            //    else
            //    {
            //        var lineObj = new LineInfo() { Line = line, TimeStamp = currentTimeStamp/*, LineNumber = currentLineNumber */};
            //        currentTimeStamp.Lines.Add(lineObj);
            //        ++currentLineNumber;
            //        _listOfLines.Add(lineObj);
            //    }
            //}
        }

        //private TimeStamp TryReadTimeStamp(string line)
        //{

        //    Match m = _regTimeStamp.Match(line);
        //    if (m.Success)
        //    {
        //        var from = ParseDateToMiliseconds(m.Groups["fromTime"].Value);
        //        var to = ParseDateToMiliseconds(m.Groups["toTime"].Value);

        //        TimeStamp ts = new TimeStamp() { FromTime = from, Duration = to - from };
        //        return ts;
        //    }
        //    return null;
        //}

        private static long ParseDateToMiliseconds(string str)
        {
            var time = DateTime.ParseExact(str, "HH:mm:ss,fff", Thread.CurrentThread.CurrentCulture);
            return time.Millisecond + time.Minute * 60 * 1000 + time.Second * 1000 + time.Hour * 60 * 60 * 1000;
        }

        //private bool CheckIsNumberingLine(string line)
        //{

        //    return _regNumbering.IsMatch(line);
        //}


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

        internal void WriteSrt(string p)
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
            foreach (var time in TimeMarkers)
            {
                sb.AppendLine(globalLineCounter.ToString());
                string strFromTime = TimespanToString(TimeSpan.FromMilliseconds(time.FromTime + time.Correction));//.ToString("hh\\:mm\\:ss\\,fff");
                string strToTime = TimespanToString(TimeSpan.FromMilliseconds(time.FromTime + time.Correction + time.Duration));//.ToString("hh\\:mm\\:ss\\,fff");
                sb.AppendLine(strFromTime + " --> " + strToTime);
                foreach (var lineinf in time.Lines)
                {
                    sb.AppendLine(lineinf.Line);
                }

                ++globalLineCounter;
            }
            File.WriteAllText(p, sb.ToString());
        }

        private string TimespanToString(TimeSpan timeSpan)
        {
            return timeSpan.Hours.ToString("00") + ":" + timeSpan.Minutes.ToString("00") + ":" + timeSpan.Seconds.ToString("00") + "," + timeSpan.Milliseconds.ToString("000");
        }
    }
}
