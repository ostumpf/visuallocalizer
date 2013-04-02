using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VisualLocalizer.Commands;
using VisualLocalizer.Gui;
using VisualLocalizer.Components;

namespace VLUnitTests.VLTests {
    
    [TestClass()]
    public class BatchMoveGridTest {

        private BatchMoveToResourcesToolGrid_Accessor grid;
        private BatchMoveToResourcesToolPanel_Accessor panel;


        [TestInitialize()]
        public void Init() {
            Agent.EnsureSolutionOpen();

            panel = new BatchMoveToResourcesToolPanel_Accessor();
            grid = new BatchMoveToResourcesToolGrid_Accessor(panel);
            grid.SetData(CSharpBatchMoveTest.GetExpectedResultsFor(Agent.CSharpStringsTestFile1).Cast<CodeStringResultItem>().ToList());
        }
        

        [TestMethod()]
        public void ShowTest() {
            
        }

    }
}
