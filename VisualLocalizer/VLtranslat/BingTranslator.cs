using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Xml;

namespace VisualLocalizer.Translate {

    /// <summary>
    /// Singleton implementation of translation service for Microsoft Bing.
    /// </summary>
    public class BingTranslator : ITranslatorService {

        private static BingTranslator instance;        

        // URI to use for requests when source language is specified
        private const string APP_URI_FULL = "https://api.datamarket.azure.com/Bing/MicrosoftTranslator/v1/Translate?Text=%27{0}%27&From=%27{1}%27&To=%27{2}%27";

        // URI to use for requests when source language is null and auto-detect takes place
        private const string APP_URI_AUTO_DETECT = "https://api.datamarket.azure.com/Bing/MicrosoftTranslator/v1/Translate?Text=%27{0}%27&To=%27{1}%27";

        // URL from where AppID can be obtained
        public const string GET_BING_APPID_URL = "https://datamarket.azure.com/dataset/bing/microsofttranslator";

        public static ITranslatorService GetService(string appid) {
            if (instance == null) instance = new BingTranslator();
            instance.AppId = appid;
            return instance;
        }

        /// <summary>
        /// Get or set Bing Application ID (can be obtained from MS store)
        /// </summary>
        public string AppId {
            get;
            set;
        }

        /// <summary>
        /// Translates the specified from language.
        /// </summary>
        /// <param name="fromLanguage">From language.</param>
        /// <param name="toLanguage">To language.</param>
        /// <param name="untranslatedText">The untranslated text.</param>  
        public string Translate(string fromLanguage, string toLanguage, string untranslatedText) {
            if (string.IsNullOrEmpty(AppId)) throw new InvalidOperationException("Cannot perform this operations with AppId being empty.");
            if (string.IsNullOrEmpty(untranslatedText)) return untranslatedText;
            if (string.IsNullOrEmpty(toLanguage)) throw new ArgumentNullException("toLanguage");

            string realUri = null;
            // is source language is empty or null, use auto-detection URI
            // fill URI with data - languages and text
            if (string.IsNullOrEmpty(fromLanguage)) {
                realUri = string.Format(APP_URI_AUTO_DETECT, Uri.EscapeUriString(untranslatedText), toLanguage);
            } else {
                realUri = string.Format(APP_URI_FULL, Uri.EscapeUriString(untranslatedText), fromLanguage, toLanguage);
            }
            
            // use AppID as authorization
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(realUri);
            request.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(AppId + ":" + AppId)));
            
            // response is in XML format
            WebResponse response = request.GetResponse();

            string translatedText = null, fullResponse = null;
            StreamReader reader = null; 

            try {
                reader = new StreamReader(response.GetResponseStream());
                fullResponse = reader.ReadToEnd();

                XmlDocument doc = new XmlDocument();
                doc.XmlResolver = null;//stop validation

                doc.LoadXml(fullResponse);

                // response XML should have one <entry> tag, which should contain one <content> tag
                XmlNodeList entriesList = doc.GetElementsByTagName("entry");
                if (entriesList.Count != 1) throw new FormatException("Web response has invalid format: exactly one <entry> expected.");

                XmlElement entry = (XmlElement)entriesList.Item(0);
                XmlNodeList contentsList = entry.GetElementsByTagName("content");
                if (contentsList.Count != 1) throw new FormatException("Web response has invalid format: exactly one <content> expected.");

                XmlElement contentElement = (XmlElement)contentsList.Item(0);

                // <content> has at least on <properties> element
                XmlElement mpropertiesElement = (XmlElement)contentElement.FirstChild;

                // <properties> element contains <text> element
                XmlElement textElement = (XmlElement)mpropertiesElement.FirstChild;

                // its inner text is translated text
                translatedText = textElement.InnerText;
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
