using System;
using System.Collections.Generic;
using System.Text;

namespace GoogleTranslator
{
   [Serializable]
   public class LanguageDetectionResponseData
   {
      public string language;
      public string isReliable;
      public string confidence;
   }
}
