using VisualLocalizer.Library;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using System.IO;
using System.Text;
using System.Collections.Generic;
using VisualLocalizer.Library.Algorithms;
using VisualLocalizer.Library.Extensions;

/// Contains unit tests for the VLlib project.
namespace VLUnitTests.VLLibTests {

    /// <summary>
    /// Tests for the customized Aho-Corasick algorithm implemented in the Trie class
    /// </summary>
    [TestClass()]
    public class TrieTest {
       
        /// <summary>
        /// Tests the Trie
        /// </summary>
        [TestMethod()]
        public void TrieConstructorTest() {
            Trie<TrieElement> trie = new Trie<TrieElement>();
            
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("VLUnitTests.Resources.TrieTest.txt");
            string text = stream.ReadAll(); // read all data from testing file

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

}
