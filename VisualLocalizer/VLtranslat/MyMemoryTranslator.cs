using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Xml;
using System.IO;

namespace VisualLocalizer.Translate {

    /// <summary>
    /// Singleton implementation of translation service for My Memory.
    /// </summary>
    public class MyMemoryTranslator : ITranslatorService {

        // URI where requests are sent
        private const string APP_URI = "http://mymemory.translated.net/api/get?q={0}&langpair={1}|{2}&of=tmx";
        private static ITranslatorService instance;

        public static ITranslatorService GetService() {
            if (instance == null) instance = new MyMemoryTranslator();            
            return instance;
        }

        public string Translate(string fromLanguage, string toLanguage, string untranslatedText) {
            if (string.IsNullOrEmpty(untranslatedText)) return untranslatedText;
            if (string.IsNullOrEmpty(toLanguage)) throw new ArgumentNullException("toLanguage");

            // source language cannot be null, because My Memory does not support detection
            if (string.IsNullOrEmpty(fromLanguage)) throw new ArgumentException("Sorry, this service does not support detection of source language.");

            string realUri = string.Format(APP_URI, Uri.EscapeUriString(untranslatedText), fromLanguage, toLanguage);            

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(realUri);
            WebResponse response = request.GetResponse();

            string translatedText = null, fullResponse=null;
            StreamReader reader = null; 
            
            try {
                reader = new StreamReader(response.GetResponseStream());
                fullResponse = reader.ReadToEnd();

                XmlDocument doc = new XmlDocument();
                doc.XmlResolver = null;//stop validation

                doc.LoadXml(fullResponse);

                // response should contain on <body> element
                XmlNodeList bodiesList = doc.GetElementsByTagName("body");
                if (bodiesList.Count != 1) throw new FormatException("Web response has invalid format: exactly one <body> expected.");

                XmlElement bodyElement = (XmlElement)bodiesList.Item(0);

                // <body> should contain at least one <tu> element (translation unit)
                XmlNodeList tuList = bodyElement.GetElementsByTagName("tu");
                if (tuList.Count < 1) throw new FormatException("Web response has invalid format: at least one <tu> expected within <body>.");

                XmlElement tuElement = (XmlElement)tuList.Item(0);

                // first <tu> element must contain precisely two <tuv> elements - text in source and target language
                XmlNodeList tuvList = tuElement.GetElementsByTagName("tuv");
                if (tuvList.Count != 2) throw new FormatException("Web response has invalid format: exactly two <tuv> expected within <tu>.");

                // get second <tuv> element (the one in target language)
                XmlElement tuvElement = (XmlElement)tuvList.Item(1);

                translatedText = tuvElement.InnerText;
            } catch (Exception ex) {
                throw new CannotParseResponseException(fullResponse, ex);
            } finally {
                if (reader != null) reader.Close();                
                if (response != null) response.Close();                
            }

            return translatedText;
        }
    }
}
