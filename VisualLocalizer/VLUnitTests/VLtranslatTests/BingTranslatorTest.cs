using VisualLocalizer.Translate;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace VLUnitTests {

    /// <summary>
    /// Tests for Microsoft Translator service.
    /// </summary>
    [TestClass()]
    public class BingTranslatorTest {

        private TestContext testContextInstance;

        public TestContext TestContext {
            get {
                return testContextInstance;
            }
            set {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion


        [TestMethod()]
        public void TranslateTest() {
            BingTranslator target = new BingTranslator(); 
            target.AppId = "BTOQcgIba2dKND+yD1r4o+Ye8rScsr8do+xOO9u+C04="; // testing AppId

            string fromLanguage = string.Empty; // set the source language
            string toLanguage = "en"; // set the target language
            string untranslatedText = "Tohle je testovací překlad.\nDalší řádek."; // text to translate
            string expected = "This is a test translation.\nThe next line."; // expected result
            string actual = target.Translate(fromLanguage, toLanguage, untranslatedText); // run translation

            Assert.AreEqual(expected, actual);

            fromLanguage = "cs"; // try the same with specifying source language
            actual = target.Translate(fromLanguage, toLanguage, untranslatedText);

            Assert.AreEqual(expected, actual);            
        }
    }
}
