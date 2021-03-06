﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VisualLocalizer.Library.Algorithms {

    /// <summary>
    /// Implementation of string trie - prefix tree. Is used in Aho-Corasick text lookup of references to resources.
    /// </summary>
    /// <typeparam name="ElementType">Type of inner nodes of the trie</typeparam>
    public class Trie<ElementType> where ElementType : TrieElement, new() {

        /// <summary>
        /// Trie root element
        /// </summary>
        public ElementType Root { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Trie{ElementType}"/> class.
        /// </summary>
        public Trie() {
            Root = new ElementType();
            Root.Word = null;
        }

        /// <summary>
        /// Creates new trie and adds every string from data
        /// </summary>        
        public Trie(IEnumerable<string> data)
            : this() {
            if (data == null) throw new ArgumentNullException("data");

            foreach (string s in data)
                Add(s);
        }

        /// <summary>
        /// Performs one step of abstract automata - currentElement is current state, c is new character.
        /// If current state has transition defined for c, the transition is performed; otherwise we go back
        /// until we find either root, or element where transition for c is defined. Returns the new element
        /// (after transition)
        /// </summary>        
        public ElementType Step(ElementType currentElement, char c) {
            if (currentElement == null) throw new ArgumentNullException("currentElement");

            if (currentElement.CanBeFollowedByWhitespace && char.IsWhiteSpace(c)) {
                return currentElement;
            } else {
                while (!currentElement.Successors.ContainsKey(c) && currentElement != Root)
                    currentElement = (ElementType)currentElement.Predecessor;
                if (currentElement.Successors.ContainsKey(c)) currentElement = (ElementType)currentElement.Successors[c];
                return currentElement;
            }
        }

        /// <summary>
        /// Should be called after having added all the strings. It creates links between the elements.
        /// </summary>
        public void CreatePredecessorsAndShortcuts() {
            Queue<ElementType> queue = new Queue<ElementType>();

            foreach (var pair in Root.Successors) {
                pair.Value.Predecessor = Root; // predecessor of every root successor is the root
                queue.Enqueue((ElementType)pair.Value);
            }

            while (queue.Count > 0) {
                ElementType i = queue.Dequeue();
                foreach (var pair in i.Successors) {
                    ElementType s = (ElementType)pair.Value;
                    ElementType z = Step((ElementType)i.Predecessor, pair.Key);
                    s.Predecessor = z;

                    if (z.IsTerminal) { // shortcuts make it possible to report results with one being part of the other
                        s.Shortcut = z;
                    } else {
                        s.Shortcut = z.Shortcut;
                    }
                    queue.Enqueue(s);
                }
            }
        }

        /// <summary>
        /// Add new string into the trie, returning new terminal element.
        /// </summary>        
        public ElementType Add(string text) {
            if (string.IsNullOrEmpty(text)) throw new ArgumentNullException("text");

            ElementType e = Root;

            // go from root, create new elements for undefined transitions
            for (int i = 0; i < text.Length; i++) {
                char c = text[i];
                
                if (e.Successors.ContainsKey(c)) { // transition exists
                    if (c == '.') e.CanBeFollowedByWhitespace = true; // after a . in a reference can be whitespace character
                    e = (ElementType)e.Successors[c];
                    if (c == '.') e.CanBeFollowedByWhitespace = true;
                } else { 
                    ElementType newElement = new ElementType();
                    newElement.Word = text.Substring(0, i + 1);

                    if (c == '.') e.CanBeFollowedByWhitespace = true;
                    e.Successors.Add(c, newElement);
                    e = newElement;
                    if (c == '.') e.CanBeFollowedByWhitespace = true;
                }
            }
            e.IsTerminal = true; // last element is set as terminal
            return e;
        }
    }

    /// <summary>
    /// Represents node of a prefix tree.
    /// </summary>
    public class TrieElement {

        /// <summary>
        /// Successor nodes specific for characters
        /// </summary>
        public Dictionary<char, TrieElement> Successors {
            get;
            private set;
        }

        /// <summary>
        /// Previous element
        /// </summary>
        public TrieElement Predecessor { get; set; }

        /// <summary>
        /// Last output element
        /// </summary>
        public TrieElement Shortcut { get; set; }

        /// <summary>
        /// True if this node represents a terminal state
        /// </summary>
        public bool IsTerminal { get; set; }

        /// <summary>
        /// True if this node can be followed by optional amount of whitespace
        /// </summary>
        public bool CanBeFollowedByWhitespace { get; set; }    
    
        /// <summary>
        /// The word of this prefix node
        /// </summary>
        public string Word { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TrieElement"/> class.
        /// </summary>
        public TrieElement() {
            Successors = new Dictionary<char, TrieElement>();                       
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString() {
            return string.Format("{0}, terminal={1}", Word, IsTerminal);
        }
    }
}
