using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VisualLocalizer.Commands {
    internal sealed class BatchMoveCommand : AbstractCommand {

        public BatchMoveCommand(VisualLocalizerPackage package)
            : base(package) {
        }

        public override void Process() {
            
        }

        public void Process(Array selectedItems) {

        }
    }
}
