using VisualLocalizer.Translate;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace VLUnitTests.VLtranslatTests {

    /// <summary>
    /// Tests for Google Translate service.
    /// </summary>
    [TestClass()]
    public class GoogleTranslatorTest {

        /// <summary>
        /// Tests if escape sequences in the response text are correctly parsed
        /// </summary>
        [TestMethod()]
        [DeploymentItem("VLtranslat.dll")]
        public void ReadJSONStringTest() {
            GoogleTranslator_Accessor target = new GoogleTranslator_Accessor(); 
            string text = "\"nejaky text\\\"aa\\\" sd\\n\\r\\t\\f \\u12af rrrr\"";
            int position = 0;
            string expected = "nejaky text\"aa\" sd\n\r\t\f \u12af rrrr";
            string actual = target.ReadJSONString(text, ref position);                        
            Assert.AreEqual(expected, actual);            
        }

        /// <summary>
        /// Tests the translation
        /// </summary>
        [TestMethod()]
        public void TranslateTest() {
            GoogleTranslator target = new GoogleTranslator();
            string fromLanguage = string.Empty;
            string toLanguage = "en";
            string untranslatedText = "Tohle je testovací překlad.\nDalší řádek.";
            string expected = "This is a test translation.\nAnother row .";
            string actual = target.Translate(fromLanguage, toLanguage, untranslatedText, true);
            
            Assert.AreEqual(expected, actual);

            fromLanguage = "cs";
            actual = target.Translate(fromLanguage, toLanguage, untranslatedText, true);

            Assert.AreEqual(expected, actual);            
        }

        [TestMethod()]
        public void TranslateTest2() {
            GoogleTranslator target = new GoogleTranslator();
            string fromLanguage = string.Empty;
            string toLanguage = "cs";
            string untranslatedText = "In this year ({0,2}), {1:d} were produced.";
            string expected = "V tomto roce ( {0,2} ) , bylo vyrobeno {1:d} .";
            string actual = target.Translate(fromLanguage, toLanguage, untranslatedText, true);

            Assert.AreEqual(expected, actual);

            fromLanguage = "en";
            actual = target.Translate(fromLanguage, toLanguage, untranslatedText, true);

            Assert.AreEqual(expected, actual);
        }
    }
}
