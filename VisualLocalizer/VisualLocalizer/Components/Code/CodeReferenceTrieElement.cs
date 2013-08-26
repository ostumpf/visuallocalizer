using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VisualLocalizer.Library;
using VisualLocalizer.Library.Algorithms;

namespace VisualLocalizer.Components.Code {

    /// <summary>
    /// Represents element in a reference trie
    /// </summary>
    public sealed class CodeReferenceTrieElement : TrieElement {

        /// <summary>
        /// Initializes a new instance of the <see cref="CodeReferenceTrieElement"/> class.
        /// </summary>
        public CodeReferenceTrieElement() {
            Infos = new List<CodeReferenceInfo>();
        }

        /// <summary>
        /// List of resource files, keys and values from which the trie element was added
        /// </summary>
        public List<CodeReferenceInfo> Infos {
            get;
            private set;
        }

    }

    /// <summary>
    /// Information about a reference to a resource
    /// </summary>
    public sealed class CodeReferenceInfo {

        /// <summary>
        /// Resource key
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Resource value
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// ResX file the reference comes from
        /// </summary>
        public ResXProjectItem Origin { get; set; }
    }
}
