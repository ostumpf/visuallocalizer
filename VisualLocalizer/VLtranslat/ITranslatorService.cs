using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VisualLocalizer.Translate {
    
    public enum TRANSLATE_PROVIDER { BING, MYMEMORY, GOOGLE }

    public interface ITranslatorService {
        string Translate(string fromLanguage, string toLanguage, string unstranslatedText);
    }
}
