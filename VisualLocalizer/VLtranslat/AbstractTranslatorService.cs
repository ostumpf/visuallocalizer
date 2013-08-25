using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

/*
 * This library provides functionality for translating texts using free online translation providers.
 * Each of these providers has its own implementation of ITranslatorService interface.
 */

namespace VisualLocalizer.Translate {
    
    /// <summary>
    /// Represents available translation services
    /// </summary>
    public enum TRANSLATE_PROVIDER { 
        /// <summary>
        /// Microsoft Translator service
        /// </summary>
        BING, 

        /// <summary>
        /// MyMemory service
        /// </summary>
        MYMEMORY, 

        /// <summary>
        /// Google Translate service
        /// </summary>
        GOOGLE 
    }

    /// <summary>
    /// Implemented by BingTranslator, GoogleTranslator and MyMemoryTranslator.
    /// </summary>
    public abstract class AbstractTranslatorService {      
  
        /// <summary>
        /// Represents format string placeholder, used when optimizing strings for translation
        /// </summary>
        protected Regex formattingRegex;

        /// <summary>
        /// Random number generator, used to generate temporary placeholders when optimizing format strings
        /// </summary>
        protected Random random;

        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractTranslatorService"/> class.
        /// </summary>
        protected AbstractTranslatorService() {
            formattingRegex = new Regex(@"(\{\d+(,(\-)?\d+)?(:\w+)?\})");
            random = new Random();
        }

        /// <summary>
        /// Translates source text from one language to another.
        /// </summary>
        /// <param name="fromLanguage">Two letter ISO code of the source language or null - in that case, source language gets detected by the translation service</param>
        /// <param name="toLanguage">Two letter ISO code of the target language</param>
        /// <param name="unstranslatedText">Text to be translated</param>
        /// <param name="optimizeSpecialSequences">True if format placeholders ({n}) should remain on their place</param>
        /// <returns>Text translated from source language to target language</returns>
        public string Translate(string fromLanguage, string toLanguage, string unstranslatedText, bool optimizeSpecialSequences) {
            Dictionary<int, string> encodeInfo = null;
            if (optimizeSpecialSequences) unstranslatedText = EncodeSequences(unstranslatedText, out encodeInfo);

            string translatedText = InternalTranslate(fromLanguage, toLanguage, unstranslatedText);

            if (optimizeSpecialSequences) translatedText = DecodeSequences(translatedText, encodeInfo);
            return translatedText;
        }


        /// <summary>
        /// Translates source text from one language to another.
        /// </summary>
        /// <param name="fromLanguage">Two letter ISO code of the source language or null - in that case, source language gets detected by the translation service</param>
        /// <param name="toLanguage">Two letter ISO code of the target language</param>
        /// <param name="unstranslatedText">Text to be translated</param>
        /// <returns>Text translated from source language to target language</returns>
        protected abstract string InternalTranslate(string fromLanguage, string toLanguage, string unstranslatedText);

        /// <summary>
        /// Encodes placeholders in given text with random numbers and returns map of these conversions
        /// </summary>           
        protected string EncodeSequences(string text, out Dictionary<int, string> encodeInfo) {
            encodeInfo = null;
            if (text == null) return null;

            encodeInfo = new Dictionary<int, string>();
            StringBuilder b = new StringBuilder(text);
            MatchCollection matches = formattingRegex.Matches(text);

            for (int i = matches.Count - 1; i >= 0; i--) { // run from the end to avoid position issues
                Match match = matches[i];
                // escape sequence '{{' 
                int bracketsCount = 0;
                int matchIndex = match.Index - 1;
                while (matchIndex >= 0 && text[matchIndex] == '{') {
                    matchIndex--;
                    bracketsCount++;
                }
                if (bracketsCount % 2 == 1) continue;

                int encodeVal = random.Next(500, 1000); // get value encoding the placeholder
                
                // replace it 
                b.Remove(match.Index, match.Length);
                b.Insert(match.Index, encodeVal);

                // remember the placeholder and its encoded value
                encodeInfo.Add(encodeVal, match.Value);
            }

            return b.ToString();
        }

        /// <summary>
        /// Replaces number in text with given replacements
        /// </summary>        
        protected string DecodeSequences(string text, Dictionary<int, string> encodeInfo) {
            if (text == null) return null;

            StringBuilder b = new StringBuilder(text);
            if (encodeInfo != null) {
                // replace all numbers that encode placeholders with the placeholders themselves
                foreach (var pair in encodeInfo) { 
                    b.Replace(pair.Key.ToString(), pair.Value);
                }
            }
            return b.ToString();
        }
    }
    
    /// <summary>
    /// Holds extension methods for the TRANSLATE_PROVIDER enum
    /// </summary>
    public static class TranslateProvidersExt {

        /// <summary>
        /// Extension method for user-friendly name of the translation provider.
        /// </summary>
        /// <param name="p"></param>
        /// <returns>User-friendly name or null in case of invalid enum value</returns>
        public static string ToHumanForm(this TRANSLATE_PROVIDER p) {
            switch (p) {
                case TRANSLATE_PROVIDER.BING:
                    return "Bing";
                case TRANSLATE_PROVIDER.MYMEMORY:
                    return "My Memory";
                case TRANSLATE_PROVIDER.GOOGLE:
                    return "Google";     
            }
            return null;
        }
    }
}
