using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SyncSubsByComparison
{
    public interface ITranslator
    {
        bool IsOperational{get;}
        IEnumerable<string> TranslateLines(IEnumerable<string> translateArraySourceTexts, string toLang, string fromLang);
        string DetectLanguage(string text);
    }
}
