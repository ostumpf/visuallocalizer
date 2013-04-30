using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

/// Contains test classes for the Visual Localizer project and its libraries.
namespace VLUnitTests {

    /// <summary>
    /// Class holding extension methods used in the unit tests.
    /// </summary>
    public static class TestExtensions {

        /// <summary>
        /// Reads all content of given stream, expecting it to be UTF-8 encoded text. Returns the text.
        /// </summary>        
        public static string ReadAll(this Stream stream) {
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
