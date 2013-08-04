using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SyncSubsByComparison
{
    public class SrtParserTranslator : ITranslator
    {
        static Regex _regTimeStamp = new Regex(@"\d+?\w*?\s+(?<fromTime>\d\d:\d\d:\d\d,\d\d\d)\s+-->\s+(?<toTime>\d\d:\d\d:\d\d,\d\d\d)(?<lines>.*?)(?=\d+?\w*?\s+\d\d:\d\d:\d\d,\d\d\d\s+-->|\z)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
        //static Regex _regNumbering = new Regex(@"\d+.*", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

        List<string> _translation;
        public SrtParserTranslator(string translation)
        {
            _translation = new List<string>();
            //_translation = translation.Split("\n\r".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Where(l => !_regTimeStamp.IsMatch(l));
            var matches = _regTimeStamp.Matches(translation);
            if (matches.Count == 0)
                return;

            foreach (Match m in matches)
            {
                _translation.AddRange(m.Groups["lines"].Value.Split("\n\r".ToCharArray(), StringSplitOptions.RemoveEmptyEntries));
            }
        }

        public IEnumerable<string> TranslateLines(IEnumerable<string> translateArraySourceTexts, string toLang, string fromLang)
        {
            if (translateArraySourceTexts.Count() != _translation.Count())
                throw new Exception("the translation lines don't match, copy the entire text from google translate!");
            else
                return _translation;
        }

        public string DetectLanguage(string text)
        {
            return "unknown";
        }

        public bool IsOperational
        {
            get { return true; }
        }
    }

    //public class PromptingTextTranslator : ITranslator
    //{
    //    public IEnumerable<string> TranslateLines(IEnumerable<string> translateArraySourceTexts, string toLang, string fromLang)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}

}
