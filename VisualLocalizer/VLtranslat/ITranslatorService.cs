using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VisualLocalizer.Translate {
    
    public enum TRANSLATE_PROVIDER { BING, MYMEMORY, GOOGLE }

    public interface ITranslatorService {
        string Translate(string fromLanguage, string toLanguage, string unstranslatedText);
    }

    public static class TranslateProvidersExt {
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
