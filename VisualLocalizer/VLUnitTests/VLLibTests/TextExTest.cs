using VisualLocalizer.Library;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;
using System;

namespace VLUnitTests.VLLibTests {
    
    /// <summary>
    /// Tests for string extension methods
    /// </summary>
    [TestClass()]
    public class TextExTest {
     
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
