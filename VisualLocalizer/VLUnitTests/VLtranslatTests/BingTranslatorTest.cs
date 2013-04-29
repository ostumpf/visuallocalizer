using VisualLocalizer.Translate;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// Contains tests for the VLtranslat library.
namespace VLUnitTests.VLtranslatTests {

    /// <summary>
    /// Tests for Microsoft Translator service.
    /// </summary>
    [TestClass()]
    public class BingTranslatorTest {
      
        [TestMethod()]
        public void TranslateTest() {
            BingTranslator target = new BingTranslator(); 
            target.AppId = "BTOQcgIba2dKND+yD1r4o+Ye8rScsr8do+xOO9u+C04="; // testing AppId

            string fromLanguage = string.Empty; // set the source language
            string toLanguage = "en"; // set the target language
            string untranslatedText = "Tohle je testovací překlad.\nDalší řádek."; // text to translate
            string expected = "This is a test translation.\nThe next line."; // expected result
            string actual = target.Translate(fromLanguage, toLanguage, untranslatedText, true); // run translation

            Assert.AreEqual(expected, actual);

            fromLanguage = "cs"; // try the same with specifying source language
            actual = target.Translate(fromLanguage, toLanguage, untranslatedText, true);

            Assert.AreEqual(expected, actual);            
        }
    }
}
