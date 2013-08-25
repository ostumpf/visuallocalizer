using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VisualLocalizer.Library.Extensions;

namespace VLUnitTests.VLTests {

    /// <summary>
    /// Tests parsing a tab-separated text
    /// </summary>
    [TestClass()]
    public class TabbedFormatTests {

        [TestMethod()]
        public void NoQuotesTest() {
            string tabbedText = "a\tb\tc\thello,world\r\nx\ty\t\r\ndd";

            List<List<string>> expected = new List<List<string>>();
            expected.Add(new List<string>() { "a", "b", "c", "hello,world" });
            expected.Add(new List<string>() { "x", "y" });
            expected.Add(new List<string>() { "dd" });
            List<List<string>> actual = tabbedText.ParseTabbedText();

            Check(expected, actual);
        }

        [TestMethod()]
        public void EmptyLinesTest() {
            string tabbedText = "\r\na\tb\r\n\r\nc\r\n";

            List<List<string>> expected = new List<List<string>>();
            expected.Add(new List<string>());
            expected.Add(new List<string>() { "a", "b" });
            expected.Add(new List<string>());
            expected.Add(new List<string>() { "c" });
            expected.Add(new List<string>());
            List<List<string>> actual = tabbedText.ParseTabbedText();

            Check(expected, actual);
        }

        [TestMethod()]
        public void QuotesTest() {
            string tabbedText = "abc\t\"de\"\t\"f\"\r\n\r\n\"aa\"\t\"\"\"\"\t\"aa\"\"bb\"";

            List<List<string>> expected = new List<List<string>>();
            expected.Add(new List<string>() { "abc", "de", "f" });
            expected.Add(new List<string>());            
            expected.Add(new List<string>() { "aa", "\"", "aa\"bb" });            
            List<List<string>> actual = tabbedText.ParseTabbedText();

            Check(expected, actual);
        }

        [TestMethod()]
        public void NewLinesTest() {
            string tabbedText = "abc\t\"de\"\t\"f\r\n\r\n\"\t\"aa\"\t\"\"\"\"\t\"aa\"\"bb\"\r\n\"\r\na\r\n\"\tb";

            List<List<string>> expected = new List<List<string>>();
            expected.Add(new List<string>() { "abc", "de", "f\r\n\r\n", "aa", "\"", "aa\"bb" });
            expected.Add(new List<string>() { "\r\na\r\n", "b"});
            List<List<string>> actual = tabbedText.ParseTabbedText();

            Check(expected, actual);
        }

        private void Check(List<List<string>> expected, List<List<string>> actual) {
            Assert.AreEqual(expected.Count, actual.Count);
            for (int i = 0; i < expected.Count; i++) {
                List<string> expColumns = expected[i];
                List<string> actColumns = actual[i];

                Assert.AreEqual(expColumns.Count, actColumns.Count);
                for (int j = 0; j < expColumns.Count; j++) {
                    Assert.AreEqual(expColumns[j], actColumns[j]);
                }
            }
        }
    }
}
