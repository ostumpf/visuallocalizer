using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VisualLocalizer.Library {
    public interface IKeyValueSource {
        string Key { get; }
        string Value { get; }
        List<IKeyValueSource> ItemsWithSameKey { get; set; }
        HashSet<IKeyValueSource> ConflictRows { get; }
        HashSet<string> ErrorSet { get; }
        void ErrorSetUpdate();
    }
}
