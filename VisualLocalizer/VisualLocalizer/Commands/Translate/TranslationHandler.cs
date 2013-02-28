using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Translate;
using VisualLocalizer.Settings;
using VisualLocalizer.Components;

namespace VisualLocalizer.Commands {
    internal static class TranslationHandler {

        public static void Translate(List<AbstractTranslateInfoItem> dict, TRANSLATE_PROVIDER provider, string from, string to) {
            ITranslatorService service = null;
            switch (provider) {
                case TRANSLATE_PROVIDER.BING:
                    service = BingTranslator.GetService(SettingsObject.Instance.BingAppId);
                    break;
                case TRANSLATE_PROVIDER.MYMEMORY:
                    service = MyMemoryTranslator.GetService();
                    break;
                case TRANSLATE_PROVIDER.GOOGLE:
                    service = GoogleTranslator.GetService();
                    break;
            }
            if (service == null) {
                throw new Exception("Cannot resolve translation provider!");
            } else {
                try {
                    ProgressBarHandler.StartDeterminate(dict.Count);

                    int completed = 0;
                    foreach (AbstractTranslateInfoItem item in dict) {
                        string oldValue = item.Value;
                        item.Value = service.Translate(from, to, oldValue);
                        completed++;

                        VLOutputWindow.VisualLocalizerPane.WriteLine("Translated \"{0}\" as \"{1}\" ", oldValue, item.Value);
                        ProgressBarHandler.SetDeterminateProgress(completed);
                    }                    
                } finally {
                    ProgressBarHandler.StopDeterminate();
                }
            }            
        }

    }
}
