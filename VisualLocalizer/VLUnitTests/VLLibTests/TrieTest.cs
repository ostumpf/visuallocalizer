using VisualLocalizer.Library;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace VLUnitTests {

    /// <summary>
    /// Tests for the customized Aho-Corasick algorithm implemented in the Trie class
    /// </summary>
    [TestClass()]
    public class TrieTest {
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
        /// Tests the Trie
        /// </summary>
        [TestMethod()]
        public void TrieConstructorTest() {
            Trie<TrieElement> trie = new Trie<TrieElement>();
            
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("VLUnitTests.Resources.TrieTest.txt");
            string text = stream.readAll(); // read all data from testing file

            // add "references" that will be searched
            trie.Add("id.aliquam.lorem"); 
            trie.Add("a.b.c.d");
            trie.Add("x.y");
            trie.Add("eeee.eeee");
            
            // complete building the trie
            trie.CreatePredecessorsAndShortcuts();

            TrieElement e = trie.Root;
            List<string> foundWords = new List<string>(); 
            
            // create list of expeced results
            List<string> expectedWords = new List<string>();
            expectedWords.Add("id.aliquam.lorem");
            expectedWords.Add("id.aliquam.lorem");
            expectedWords.Add("eeee.eeee");
            expectedWords.Add("id.aliquam.lorem");            
            expectedWords.Add("eeee.eeee");
            expectedWords.Add("eeee.eeee");
            expectedWords.Add("eeee.eeee");
            expectedWords.Add("a.b.c.d");
            expectedWords.Add("a.b.c.d");
            expectedWords.Add("a.b.c.d");
            expectedWords.Add("a.b.c.d");
            expectedWords.Add("a.b.c.d");

            // run the algorithm
            foreach (char c in text) {
                e = trie.Step(e, c);
                if (e.IsTerminal) foundWords.Add(e.Word);
            }

            // compare with expected
            if (foundWords.Count == expectedWords.Count) {
                bool ok = true;
                for (int i = 0; i < foundWords.Count; i++) {
                    ok = ok && foundWords[i] == expectedWords[i];
                }
                Assert.IsTrue(ok);
            } else Assert.Fail("Found and expected words count don't match.");
        }        
    }

    public static class Ext {
        public static string readAll(this Stream stream) {
            byte[] buffer = new byte[1024];
            int count = 0;
            StringBuilder b = new StringBuilder();

            while ((count = stream.Read(buffer, 0, buffer.Length)) > 0) {
                b.Append(Encoding.UTF8.GetString(buffer, 0, count));
            }

            return b.ToString();
        }
    }
}
