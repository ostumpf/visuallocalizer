using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;

namespace VisualLocalizer.Translate {

    /// <summary>
    /// Singleton implementation of translation service for Google Translate.
    /// </summary>
    public class GoogleTranslator : ITranslatorService {

        private static ITranslatorService service;

        // URI where requests are sent
        private const string APP_URI = "http://translate.google.com/translate_a/t?client=t&text={0}&sl={1}&tl={2}&ie=UTF-8&oe=UTF-8";

        public static ITranslatorService GetService() {
            if (service == null) service = new GoogleTranslator();
            return service;
        }

        /// <summary>
        /// Translates the specified from language.
        /// </summary>
        /// <param name="fromLanguage">From language.</param>
        /// <param name="toLanguage">To language.</param>
        /// <param name="untranslatedText">The untranslated text.</param> 
        public string Translate(string fromLanguage, string toLanguage, string untranslatedText) {
            if (string.IsNullOrEmpty(untranslatedText)) return untranslatedText;
            if (string.IsNullOrEmpty(toLanguage)) throw new ArgumentNullException("toLanguage");
            
            // when source language is null, correspoding field in the URI should be blank
            string realUri = string.Format(APP_URI, Uri.EscapeUriString(untranslatedText), fromLanguage, toLanguage);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(realUri);
            WebResponse response = request.GetResponse();

            string translatedText = null, fullResponse = null;
            StreamReader reader = null;

            try {
                reader = new StreamReader(response.GetResponseStream());
                fullResponse = reader.ReadToEnd();

                // response is in JSON format - an array

                int i = 0;
                List<object> resp = ReadJSONArray(fullResponse, ref i);
                List<object> first = (List<object>)resp[0];
                foreach (List<object> d in first) {
                    translatedText += (string)d[0];
                }

            } catch (Exception ex) {
                throw new CannotParseResponseException(fullResponse, ex);
            } finally {
                if (reader != null) reader.Close();
                if (response != null) response.Close();
            } 
            
            return translatedText;
        }

        /// <summary>
        /// Parses JSON array from text and returns list of the elements. The elements can be either strings or another lists of objects.
        /// </summary>
        /// <param name="text">JSON text</param>
        /// <param name="position">Position where array starts</param>        
        private List<object> ReadJSONArray(string text, ref int position) {
            ReadChar(text, ref position, '[');
            List<object> list = new List<object>();

            while (true) {
                if (GetAt(text, position) != ']' && GetAt(text, position) != ',') { // end of array
                    list.Add(ReadJSONElement(text, ref position));
                    ReadWhitespace(text, ref position);
                }
                if (GetAt(text, position)==']') {
                    break; // end of array
                } else if (GetAt(text, position) == ',') {
                    position++;
                } else {
                    throw new Exception("JSON parser error, expected ',' or ']'");
                }
            }

            ReadChar(text, ref position, ']');

            return list;
        }

        /// <summary>
        /// Reads JSON input, applying appropriate methods based on read content.
        /// </summary>        
        private object ReadJSONElement(string text, ref int position) {
            ReadWhitespace(text, ref position);
            char? c = GetAt(text, position);

            if (c == '"') { // it's a string
                return ReadJSONString(text, ref position);
            } else if (c == '{') { // it's an object - these do not appear in Google response, so we can ignore them
            } else if (c == '[') { // it's an array
                return ReadJSONArray(text, ref position);
            } else {
                // skip content, until another delimiter is found
                while (GetAt(text, position) != ',' && GetAt(text, position) != ']' && GetAt(text, position) != '}') {
                    position++;
                }
            }
            return "";
        }

        /// <summary>
        /// Reads JSON string, replacing standard escape sequences with appropriate characters.
        /// </summary>        
        private string ReadJSONString(string text, ref int position) {
            ReadChar(text, ref position, '"'); // starting "
            StringBuilder b = new StringBuilder();

            while (true) { // read characters from input text
                if (!GetAt(text, position).HasValue) break; // end of text - break

                char c = GetAt(text, position).Value;
                bool print = true;

                if (c == '"') break; 
                if (c == '\\') { // escape sequence start
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
                        case 'u': position++; b.Append(ReadHexCode(text, ref position)); print = false; break;    
                    }
                }

                if (print) {
                    b.Append(c);
                    position++;
                }
            }

            ReadChar(text, ref position, '"'); // ending "
            return b.ToString();
        }

        /// <summary>
        /// Reads 4 characters from text, expecting them to be hex digits encoding a character. Returns character with that code.
        /// </summary>        
        private char ReadHexCode(string text, ref int position) {
            int sum = 0;
            int end = position + 4;
            for (; position < end; position++ ) {
                char? c = GetAt(text, position);
                if (!c.HasValue) throw new Exception("JSON parse exception - expecting four hex digits at " + position + ".");

                sum = sum * 16 + GetNumValueOf(c.Value);
            }

            return (char)sum;
        }

        /// <summary>
        /// Returns 0-15 value of passed hexadecimal character.
        /// </summary>        
        private int GetNumValueOf(char p) {
            if (p >= '0' && p <= '9') {
                return p - '0';
            } else {
                switch (char.ToLower(p)) {
                    case 'a': return 10;
                    case 'b': return 11;
                    case 'c': return 12;
                    case 'd': return 13;
                    case 'e': return 14;
                    case 'f': return 15;
                    default: throw new Exception("JSON parse exception - expecting hex digit, not '"+p+"'.");
                }
            }
        }

        /// <summary>        
        /// Reads whitespace from text, starting at given position and compares first non-whitespace character with expected character c.
        /// If they're not equal, exception is thrown.
        /// </summary>        
        private void ReadChar(string text, ref int position, char expectedChar) {
            ReadWhitespace(text, ref position);
            
            char? currentChar = GetAt(text, position);
            if (!currentChar.HasValue || currentChar.Value != expectedChar) throw new Exception("JSON parser error, expected '" + expectedChar + "'");

            position++;
        }

        /// <summary>
        /// Reads characters from text as long as they're whitespace.
        /// </summary>        
        private void ReadWhitespace(string text, ref int position) {
            char? c = GetAt(text, position);
            while (c.HasValue && char.IsWhiteSpace(c.Value)) {
                position++;
                c = GetAt(text, position);
            }
        }

        /// <summary>
        /// Attempts to get character at position from text - returns null when position index is outside range.
        /// </summary>        
        private char? GetAt(string text, int position) {
            if (position >= 0 && position < text.Length) {
                return text[position];
            } else {
                return null;
            }
        }
    }
}
