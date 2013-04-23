using VisualLocalizer.Library;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;
using System;
namespace VLUnitTests {
    
    /// <summary>
    /// Tests for string extension methods
    /// </summary>
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


        /// <summary>
        /// Since VB .NET and ASP .NET escape sequences can be trivialy implemented, the only thing that must be tested are C# strings.
        /// </summary>
        [TestMethod()]
        public void ConvertCSharpEscapeSequencesTest() {
            StringBuilder b = new StringBuilder();
            Random rnd = new Random();
            
            // create random string
            for (int i = 0; i < 10000000; i++) {
                b.Append((char)rnd.Next(200));
            }

            string testString = b.ToString();
            string escapedString = testString.ConvertCSharpUnescapeSequences(); // create escape sequences in the string
            string unescapedString = escapedString.ConvertCSharpEscapeSequences(false); // unescpae the sequences back

            // the result should be the same as the original string
            Assert.AreEqual(unescapedString, testString);
        }
    }
}
