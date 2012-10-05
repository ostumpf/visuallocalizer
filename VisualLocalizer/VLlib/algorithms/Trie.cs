using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VisualLocalizer.Library {
    public class Trie<ElementType> where ElementType:TrieElement,new() {

        public ElementType Root { get; private set; }

        public Trie() {
            Root = new ElementType();
            Root.Word = null;
        }

        public Trie(IEnumerable<string> data) : this() {            
            foreach (string s in data)
                Add(s);           
        }

        public ElementType Step(ElementType currentElement, char c) {
            while (!currentElement.Successors.ContainsKey(c) && currentElement != Root) currentElement = (ElementType)currentElement.Predecessor;
            if (currentElement.Successors.ContainsKey(c)) currentElement = (ElementType)currentElement.Successors[c];
            return currentElement;
        }

        public void CreatePredecessorsAndShortcuts() {
            Queue<ElementType> queue = new Queue<ElementType>();

            foreach (var pair in Root.Successors) {
                pair.Value.Predecessor = Root;
                queue.Enqueue((ElementType)pair.Value);
            }

            while (queue.Count > 0) {
                ElementType i = queue.Dequeue();
                foreach (var pair in i.Successors) {
                    ElementType s = (ElementType)pair.Value;
                    ElementType z = Step((ElementType)i.Predecessor, pair.Key);
                    s.Predecessor = z;

                    if (z.IsTerminal) {
                        s.Shortcut = z;
                    } else {
                        s.Shortcut = z.Shortcut;
                    }
                    queue.Enqueue(s);
                }
            }
        }

        public ElementType Add(string text) {
            ElementType e = Root;

            for (int i = 0; i < text.Length; i++) {
                char c = text[i];
                if (e.Successors.ContainsKey(c)) {
                    e = (ElementType)e.Successors[c];
                } else {
                    ElementType newElement = new ElementType();
                    newElement.Word = text.Substring(0, i + 1);
                    e.Successors.Add(c, newElement);
                    e = newElement;
                }
            }
            e.IsTerminal = true;
            return e;
        }
    }

    public class TrieElement {

        public Dictionary<char, TrieElement> Successors {
            get;
            private set;
        }

        public TrieElement Predecessor { get; set; }
        public TrieElement Shortcut { get; set; }
        public bool IsTerminal { get; set; }
        public string Word { get; set; }      

        public TrieElement() {
            Successors = new Dictionary<char, TrieElement>();                       
        }

        public override string ToString() {
            return string.Format("{0}, terminal={1}", Word, IsTerminal);
        }
    }
}
