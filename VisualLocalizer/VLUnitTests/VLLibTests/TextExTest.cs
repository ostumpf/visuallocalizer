using VisualLocalizer.Library;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;
using System;
namespace VLUnitTests
{
    [TestClass()]
    public class TextExTest {


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
        public void ConvertCSharpEscapeSequencesTest() {
            StringBuilder b = new StringBuilder();
            Random rnd = new Random();
            
            for (int i = 0; i < 10000000; i++) {
                b.Append((char)rnd.Next(200));
            }

            string testString = b.ToString();
            string escapedString = testString.ConvertCSharpUnescapeSequences();
            string unescapedString = escapedString.ConvertCSharpEscapeSequences(false);

            Assert.AreEqual(unescapedString, testString);
        }
    }
}
