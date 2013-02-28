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

                int i = 0;
                List<object> resp = ReadJSONArray(fullResponse, ref i);
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

        private List<object> ReadJSONArray(string text, ref int position) {
            ReadChar(text, ref position, '[');
            List<object> list = new List<object>();

            while (true) {
                if (GetAt(text, position) != ']' && GetAt(text, position) != ',') {
                    list.Add(ReadJSONElement(text, ref position));
                    ReadWhitespace(text, ref position);
                }
                if (GetAt(text, position)==']') {
                    break;
                } else if (GetAt(text, position) == ',') {
                    position++;
                } else {
                    throw new Exception("JSON parser error, expected ',' or ']'");
                }
            }

            ReadChar(text, ref position, ']');

            return list;
        }

        private object ReadJSONElement(string text, ref int position) {
            ReadWhitespace(text, ref position);
            char? c = GetAt(text, position);

            if (c == '"') {
                return ReadJSONString(text, ref position);
            } else if (c == '{') {
            } else if (c == '[') {
                return ReadJSONArray(text, ref position);
            } else {
                while (GetAt(text, position) != ',' && GetAt(text, position) != ']' && GetAt(text, position) != '}') {
                    position++;
                }
            }
            return "";
        }

        private string ReadJSONString(string text, ref int position) {
            ReadChar(text, ref position, '"');
            StringBuilder b = new StringBuilder();

            while (true) {
                char c = GetAt(text, position).Value;
                bool print = true;

                if (c == '"') break;
                if (c == '\\') {
                    position++;
                    char? next = GetAt(text, position);
                    switch (next) {
                        case '"': b.Append('"'); position++; print = false; break;
                        case '/': b.Append('/'); position++; print = false; break;
                        case '\\': b.Append('\\'); position++; print = false; break;
                        case 'r': b.Append('\r'); position++; print = false; break;
                        case 'f': b.Append('\f'); position++; print = false; break;
                        case 't': b.Append('\t'); position++; print = false; break;
                        case 'b': b.Append('\b'); position++; print = false; break;
                        case 'n': b.Append('\n'); position++; print = false; break;                        
                    }
                }

                if (print) {
                    b.Append(c);
                    position++;
                }
            }
            ReadChar(text, ref position, '"');
            return b.ToString();
        }

        private void ReadChar(string text, ref int position, char c) {
            ReadWhitespace(text, ref position);
            if (GetAt(text, position) != c) throw new Exception("JSON parser error, expected '" + c + "'");
            position++;
        }

        private void ReadWhitespace(string text, ref int position) {
            while (char.IsWhiteSpace(GetAt(text, position).Value))
                position++;
        }

        private char? GetAt(string text, int position) {
            if (position >= 0 && position < text.Length) {
                return text[position];
            } else {
                return null;
            }
        }
    }
}
