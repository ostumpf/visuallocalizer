using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;

namespace VisualLocalizer.Translate {
    public class GoogleTranslator : ITranslatorService {

        private static ITranslatorService service;
        private const string APP_URI = "http://translate.google.com/translate_a/t?client=t&text={0}&sl={1}&tl={2}&ie=UTF-8&oe=UTF-8";

        public static ITranslatorService GetService() {
            if (service == null) service = new GoogleTranslator();
            return service;
        }

        public string Translate(string fromLanguage, string toLanguage, string untranslatedText) {
            if (string.IsNullOrEmpty(untranslatedText)) return untranslatedText;
            if (string.IsNullOrEmpty(toLanguage)) throw new ArgumentNullException("toLanguage");
            
            string realUri = string.Format(APP_URI, Uri.EscapeUriString(untranslatedText), fromLanguage, toLanguage);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(realUri);
            WebResponse response = request.GetResponse();

            string translatedText = null, fullResponse = null;
            StreamReader reader = null;

            try {
                reader = new StreamReader(response.GetResponseStream());
                fullResponse = reader.ReadToEnd();
                List<object> resp = ParseJSONArray(fullResponse);
                List<object> first = (List<object>)resp[0];
                List<object> firstFirst = (List<object>)first[0];
                translatedText = (string)firstFirst[0];

            } catch (Exception ex) {
                throw new CannotParseResponseException(fullResponse, ex);
            } finally {
                if (reader != null) reader.Close();
                if (response != null) response.Close();
            }

            return translatedText;
        }

        private List<object> ParseJSONArray(string fullResponse) {
            string delimitedElements = fullResponse.Substring(1, fullResponse.Length - 2);
            string element = null;
            int position = 0;
            List<object> objects = new List<object>();

            while ((element = ReadJSONElement(delimitedElements, ref position)) != null) {
                if (element.StartsWith("[") && element.EndsWith("]")) {                    
                    objects.Add(ParseJSONArray(element));
                } else {
                    objects.Add(element);
                }
            }

            return objects;
        }

        private string ReadJSONElement(string delimitedElements, ref int position) {
            int priority = 0;
            string element = null;
            int start = position;

            while (position < delimitedElements.Length) {
                if (delimitedElements[position] == '[') {
                    priority++;
                } else if (delimitedElements[position] == ']') {
                    priority--;
                } else if (delimitedElements[position] == ',' && priority == 0) {
                    element = delimitedElements.Substring(start, position - start);
                    position++;
                    break;
                } 
                if (position == delimitedElements.Length - 1) {
                    element = delimitedElements.Substring(start);
                }
                position++;
            }

            if (element!=null && element.StartsWith("\"") && element.EndsWith("\"")) {
                element = element.Substring(1, element.Length - 2);
            }

            return element;
        }
    }
}
