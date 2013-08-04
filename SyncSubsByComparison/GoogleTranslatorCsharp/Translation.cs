using System;
using System.Collections.Generic;
using System.Text;

namespace GoogleTranslator
{
   [Serializable]
   public class Translation: JSONResponse
   {
      public TranslationResponseData responseData = new TranslationResponseData();
   }

   
}
