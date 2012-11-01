using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Xml;

namespace VisualLocalizer.Translate {
    public class BingTranslator : ITranslatorService {

        private static BingTranslator instance;        
        private const string APP_URI_FULL = "https://api.datamarket.azure.com/Data.ashx/Bing/MicrosoftTranslator/v1/Translate?Text=%27{0}%27&From=%27{1}%27&To=%27{2}%27&$top=100";
        private const string APP_URI_AUTO_DETECT = "https://api.datamarket.azure.com/Data.ashx/Bing/MicrosoftTranslator/v1/Translate?Text=%27{0}%27&To=%27{1}%27&$top=100";

        public static ITranslatorService GetService(string appid) {
            if (instance == null) instance = new BingTranslator();
            instance.AppId = appid;
            return instance;
        }

        public string AppId {
            get;
            set;
        }    

        public string Translate(string fromLanguage, string toLanguage, string untranslatedText) {
            if (string.IsNullOrEmpty(AppId)) throw new InvalidOperationException("Cannot perform this operations with AppId being empty.");
            if (string.IsNullOrEmpty(untranslatedText)) return untranslatedText;
            if (string.IsNullOrEmpty(toLanguage)) throw new ArgumentNullException("toLanguage");

            string realUri = null;
            if (string.IsNullOrEmpty(fromLanguage)) {
                realUri = string.Format(APP_URI_AUTO_DETECT, Uri.EscapeUriString(untranslatedText), toLanguage);
            } else {
                realUri = string.Format(APP_URI_FULL, Uri.EscapeUriString(untranslatedText), fromLanguage, toLanguage);
            }
            
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(realUri);
            request.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(AppId + ":" + AppId)));
            WebResponse response = request.GetResponse();

            string translatedText = null, fullResponse = null;
            StreamReader reader = null; 

            try {
                reader = new StreamReader(response.GetResponseStream());
                fullResponse = reader.ReadToEnd();

                XmlDocument doc = new XmlDocument();
                doc.XmlResolver = null;//stop validation

                doc.LoadXml(fullResponse);
                XmlNodeList entriesList = doc.GetElementsByTagName("entry");
                if (entriesList.Count != 1) throw new FormatException("Web response has invalid format: exactly one <entry> expected.");

                XmlElement entry = (XmlElement)entriesList.Item(0);
                XmlNodeList contentsList = entry.GetElementsByTagName("content");
                if (contentsList.Count != 1) throw new FormatException("Web response has invalid format: exactly one <content> expected.");

                XmlElement contentElement = (XmlElement)contentsList.Item(0);
                XmlElement mpropertiesElement = (XmlElement)contentElement.FirstChild;
                XmlElement textElement = (XmlElement)mpropertiesElement.FirstChild;

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
