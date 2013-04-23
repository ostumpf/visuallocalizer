using VisualLocalizer.Translate;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace VLUnitTests {

    /// <summary>
    /// Tests for MyMemory translation service
    /// </summary>
    [TestClass()]
    public class MyMemoryTranslatorTest {


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
            string actual = target.Translate(fromLanguage, toLanguage, untranslatedText);

            Assert.AreEqual(expected, actual);                          
        }
    }
}
