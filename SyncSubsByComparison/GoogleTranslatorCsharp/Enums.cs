using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace GoogleTranslator
{
   public class StringValueAttribute : Attribute
   {
      public string StringValue;

      public StringValueAttribute(string value)
      {
         this.StringValue = value;
      }
   }

   public static class EnumStringUtil
   {
      public static string GetStringValue(Enum value)
      {
         // Get the type
         Type type = value.GetType();

         // Get fieldinfo for this type
         FieldInfo fieldInfo = type.GetField(value.ToString());
         // Get the stringvalue attributes
         StringValueAttribute[] attribs = fieldInfo.GetCustomAttributes(
             typeof(StringValueAttribute), false) as StringValueAttribute[];
         // Return the first if there was a match.
         return attribs.Length > 0 ? attribs[0].StringValue : null;
      }

   }

   public enum VERSION
   {
      [StringValue("1.0")]ONE_POINT_ZERO
   }

   public enum LANGUAGE
   {
   //   AFRIKAANS = "af",
   //   ALBANIAN = "sq",
   //   AMHARIC = "am",
   //   ARABIC = "ar",
   //   ARMENIAN = "hy",
   //   AZERBAIJANI = "az",
   //   BASQUE = "eu",
   //   BELARUSIAN = "be",
   //   BENGALI = "bn",
   //   BIHARI = "bh",
   //   BULGARIAN = "bg",
   //   BURMESE = "my",
   //   CATALAN = "ca",
   //   CHEROKEE = "chr",
   //   CHINESE = "zh",
   //   CHINESE_SIMPLIFIED = "zh-CN",
   //   CHINESE_TRADITIONAL = "zh-TW",
   //   CROATIAN = "hr",
   //   CZECH = "cs",
   //   DANISH = "da",
   //   DHIVEHI = "dv",
   //   DUTCH = "nl",
      [StringValue("en")] ENGLISH = 12,
   //   ESPERANTO = "eo",
   //   ESTONIAN = "et",
   //   FILIPINO = "tl",
   //   FINNISH = "fi",
      [StringValue("fr")]
      FRENCH,
   //   GALICIAN = "gl",
   //   GEORGIAN = "ka",
      [StringValue("de")]
      GERMAN,
   //   GREEK = "el",
   //   GUARANI = "gn",
   //   GUJARATI = "gu",
      [StringValue("iw")] HEBREW = 18,
   //   HINDI = "hi",
   //   HUNGARIAN = "hu",
   //   ICELANDIC = "is",
   //   INDONESIAN = "id",
   //   INUKTITUT = "iu",
      [StringValue("it")]
      ITALIAN,
   //   JAPANESE = "ja",
   //   KANNADA = "kn",
   //   KAZAKH = "kk",
   //   KHMER = "km",
   //   KOREAN = "ko",
   //   KURDISH = "ku",
   //   KYRGYZ = "ky",
   //   LAOTHIAN = "lo",
   //   LATVIAN = "lv",
   //   LITHUANIAN = "lt",
   //   MACEDONIAN = "mk",
   //   MALAY = "ms",
   //   MALAYALAM = "ml",
   //   MALTESE = "mt",
   //   MARATHI = "mr",
   //   MONGOLIAN = "mn",
   //   NEPALI = "ne",
   //   NORWEGIAN = "no",
   //   ORIYA = "or",
   //   PASHTO = "ps",
   //   PERSIAN = "fa",
   //   POLISH = "pl",
   //   PORTUGUESE = "pt-PT",
   //   PUNJABI = "pa",
   //   ROMANIAN = "ro",
      [StringValue("ru")]
      RUSSIAN,
   //   SANSKRIT = "sa",
   //   SERBIAN = "sr",
   //   SINDHI = "sd",
   //   SINHALESE = "si",
   //   SLOVAK = "sk",
   //   SLOVENIAN = "sl",
      [StringValue("es")]
      SPANISH,
   //   SWAHILI = "sw",
   //   SWEDISH = "sv",
   //   TAJIK = "tg",
   //   TAMIL = "ta",
   //   TAGALOG = "tl",
   //   TELUGU = "te",
   //   THAI = "th",
   //   TIBETAN = "bo",
   //   TURKISH = "tr",
   //   UKRAINIAN = "uk",
   //   URDU = "ur",
   //   UZBEK = "uz",
   //   UIGHUR = "ug",
   //   VIETNAMESE = "vi",
   //   UNKNOWN = ""
   }

   public enum BASEURL
   {
      [StringValue("http://ajax.googleapis.com/ajax/services/language/translate")]
         TRANSLATE = 0,
      [StringValue("http://ajax.googleapis.com/ajax/services/language/detect")]
         DETECT = 1
   }
}
