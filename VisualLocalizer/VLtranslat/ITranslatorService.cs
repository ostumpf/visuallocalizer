using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/*
 * This library provides functionality for translating texts using free online translation providers.
 * Each of these providers has its own implementation of ITranslatorService interface.
 * 
 */
namespace VisualLocalizer.Translate {
    
    public enum TRANSLATE_PROVIDER { BING, MYMEMORY, GOOGLE }

    /// <summary>
    /// Implemented by BingTranslator, GoogleTranslator and MyMemoryTranslator.
    /// </summary>
    public interface ITranslatorService {
        /// <summary>
        /// Translates source text from one language to another.
        /// </summary>
        /// <param name="fromLanguage">Two letter ISO code of the source language or null - in that case, source language gets detected by the translation service</param>
        /// <param name="toLanguage">Two letter ISO code of the target language</param>
        /// <param name="unstranslatedText">Text to be translated</param>
        /// <returns>Text translated from source language to target language</returns>
        string Translate(string fromLanguage, string toLanguage, string unstranslatedText);
    }
    
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
