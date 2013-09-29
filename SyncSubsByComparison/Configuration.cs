using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace SyncSubsByComparison
{
    public class Configuration
    {
        private static Configuration _instance = new Configuration();

        public static Configuration Instance
        {
            get { return _instance; }
        }
        public static void Save(string fileName)
        {
            XmlSerializer ser = new XmlSerializer(typeof(Configuration));
            using (var stream = File.OpenWrite(fileName))
            {
                ser.Serialize(stream, _instance);
            }
        }

        public static void Load(string fileName)
        {
            if (!File.Exists(fileName))
                return;

            XmlSerializer ser = new XmlSerializer(typeof(Configuration));
            using (var stream = File.OpenRead(fileName))
            {
                _instance = (Configuration)ser.Deserialize(stream);
            }
        }

        public string MicrosoftTranslatorClientID { get; set; }

        public string MicrosoftTranslatorSecret { get; set; }

        public string LanguageSrtFilename { get; set; }

        public string TimingSrtFilename { get; set; }
    }
}
