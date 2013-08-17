using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SyncSubsByComparison
{
    public class SrtParserTranslator : ITranslator
    {
        List<string> _translation;
        public SrtParserTranslator(string translation)
        {
            SubtitleInfo inf = new SubtitleInfo(null);
            inf.LoadSrtFileContent(translation);
            _translation = inf.Lines.Select(l => l.Line).ToList();
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



}
