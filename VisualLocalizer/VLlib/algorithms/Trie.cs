using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VisualLocalizer.Library {
    public class Trie {

        public TrieElement Root { get; private set; }

        public Trie() {
            Root = new TrieElement(null);
        }

        public Trie(IEnumerable<string> data) : this() {            
            foreach (string s in data)
                Add(s,null);           
        }

        public TrieElement Step(TrieElement currentElement, char c) {
            while (!currentElement.Successors.ContainsKey(c) && currentElement != Root) currentElement = currentElement.Predecessor;
            if (currentElement.Successors.ContainsKey(c)) currentElement = currentElement.Successors[c];
            return currentElement;
        }

        public void CreatePredecessorsAndShortcuts() {
            Queue<TrieElement> queue = new Queue<TrieElement>();

            foreach (var pair in Root.Successors) {
                pair.Value.Predecessor = Root;
                queue.Enqueue(pair.Value);
            }

            while (queue.Count > 0) {
                TrieElement i = queue.Dequeue();
                foreach (var pair in i.Successors) {
                    TrieElement s = pair.Value;
                    TrieElement z = Step(i.Predecessor, pair.Key);
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

        public void Add(string text,object tag) {
            TrieElement e = Root;

            for (int i = 0; i < text.Length; i++) {
                char c = text[i];
                if (e.Successors.ContainsKey(c)) {
                    e = e.Successors[c];
                } else {
                    TrieElement newElement = new TrieElement(text.Substring(0, i + 1));                    
                    e.Successors.Add(c, newElement);
                    e = newElement;
                }
            }
            e.IsTerminal = true;
            e.Tag.Add(tag);
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
        public string Word { get; private set; }
        public List<object> Tag { get; set; }

        public TrieElement(string word) {
            Successors = new Dictionary<char, TrieElement>();
            Tag = new List<object>(1);
            this.Word = word;
        }

        public override string ToString() {
            return string.Format("{0}, terminal={1}", Word, IsTerminal);
        }
    }
}
