using VisualLocalizer.Translate;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace VLUnitTests.VLtranslatTests {

    /// <summary>
    /// Tests for MyMemory translation service
    /// </summary>
    [TestClass()]
    public class MyMemoryTranslatorTest {

        /// <summary>
        /// Tests translation - source language must be exactly specified since auto-detection is not supported
        /// </summary>
        [TestMethod()]
        public void TranslateTest() {
            MyMemoryTranslator target = new MyMemoryTranslator();
            string fromLanguage = "cs";
            string toLanguage = "en";
            string untranslatedText = "Tohle je testovací překlad.\nDalší řádek.";
            string expected = "This is a test translation. The next line.";
            string actual = target.Translate(fromLanguage, toLanguage, untranslatedText, true);

            Assert.AreEqual(expected, actual);                          
        }
    }
}
