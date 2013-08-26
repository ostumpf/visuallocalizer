using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace VisualLocalizer.Library.Extensions {

    /// <summary>
    /// Class holding extension methods working with Stream objects.
    /// </summary>
    public static class StreamExtensions {

        /// <summary>
        /// Reads all content of given stream, expecting it to be UTF-8 encoded text. Returns the text.
        /// </summary>        
        public static string ReadAll(this Stream stream) {
            return stream.ReadAll(Encoding.UTF8);
        }

        /// <summary>
        /// Reads all content of given stream, expecting it to be UTF-8 encoded text. Returns the text.
        /// </summary>        
        public static string ReadAll(this Stream stream, Encoding encoding) {
            if (stream == null) throw new ArgumentNullException("stream");
            if (encoding == null) throw new ArgumentNullException("encoding");

            byte[] buffer = new byte[1024];
            int count = 0;
            StringBuilder b = new StringBuilder();

            while ((count = stream.Read(buffer, 0, buffer.Length)) > 0) {
                b.Append(encoding.GetString(buffer, 0, count));
            }

            return b.ToString();
        }
    }
}
