using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell;
using EnvDTE;
using VisualLocalizer.Extensions;
using VisualLocalizer.Library;
using System.IO;
using VSLangProj;
using VisualLocalizer.Commands;

namespace VLUnitTests {

    /// <summary>
    /// Helper class containing methods to control testing instance of Visual Studio and paths of testing files.
    /// </summary>
    internal class Agent {
        public static EnvDTE80.DTE2 GetDTE() {
            return (EnvDTE80.DTE2)Package.GetGlobalService(typeof(EnvDTE.DTE));   
        }

        public static EnvDTE.UIHierarchy GetUIHierarchy() {
            return (EnvDTE.UIHierarchy)GetDTE().Windows.Item(EnvDTE.Constants.vsWindowKindSolutionExplorer).Object;
        }

        /// <summary>
        /// Opens the solution if not already opened
        /// </summary>
        public static void EnsureSolutionOpen() {
            var DTE = Agent.GetDTE();
            if (DTE.Solution.FileName != TestSolutionFile) {
                string s = DTE.Solution.FileName;
                DTE.Solution.Open(TestSolutionFile);
            }
        }
        
        private static BatchMoveCommand _BatchMoveCommand;

        /// <summary>
        /// Returns instance of the BatchMoveCommand
        /// </summary>
        public static BatchMoveCommand BatchMoveCommand {
            get {
                if (_BatchMoveCommand == null) _BatchMoveCommand = new BatchMoveCommand();
                return _BatchMoveCommand;
            }
        }
        
        private static BatchMoveCommand_Accessor _BatchMoveCommand_Accessor;

        /// <summary>
        /// Returns instance of the BatchMoveCommand Accessor
        /// </summary>
        public static BatchMoveCommand_Accessor BatchMoveCommand_Accessor {
            get {
                if (_BatchMoveCommand_Accessor == null) _BatchMoveCommand_Accessor = new BatchMoveCommand_Accessor();
                return _BatchMoveCommand_Accessor;
            }
        }

        private static BatchInlineCommand _BatchInlineCommand;

        /// <summary>
        /// Returns instance of the BatchInlineCommand
        /// </summary>
        public static BatchInlineCommand BatchInlineCommand {
            get {
                if (_BatchInlineCommand == null) _BatchInlineCommand = new BatchInlineCommand();
                return _BatchInlineCommand;
            }
        }

        private static BatchInlineCommand_Accessor _BatchInlineCommand_Accessor;

        /// <summary>
        /// Returns instance of the BatchInlineCommand_Accessor
        /// </summary>
        public static BatchInlineCommand_Accessor BatchInlineCommand_Accessor {
            get {
                if (_BatchInlineCommand_Accessor == null) _BatchInlineCommand_Accessor = new BatchInlineCommand_Accessor();
                return _BatchInlineCommand_Accessor;
            }
        }

        /// <summary>
        /// Searches given UIHieararchy recursively for UIHierarchyItem with specified path
        /// </summary>        
        public static UIHierarchyItem FindUIHierarchyItem(UIHierarchyItems list, string path) {
            if (list == null) return null;
            if (!list.Expanded) list.Expanded = true;

            UIHierarchyItem result = null;
            foreach (UIHierarchyItem item in list) {
                if (item.Object is ProjectItem) {
                    ProjectItem p = (ProjectItem)item.Object;                    
                    result = ComparePathsSearch(p.GetFullPath(), path, item);
                } else if (item.Object is Project) {
                    Project p = (Project)item.Object;                    
                    result = ComparePathsSearch(p.FileName, path, item);
                } else if (item.Object is Solution) {
                    Solution s = (Solution)item.Object;                    
                    result = ComparePathsSearch(s.FileName, path, item);
                } else if (item.Object is UIHierarchyItem) {
                    ComparePathsSearch(item.Name, path, item);
                } else if (item.Object is Reference) {
                    
                } else throw new Exception(item.Object.GetVisualBasicType());

                if (result != null) break;
            }
            return result;
        }

        /// <summary>
        /// Compares the two file paths and if not equal, recursively searches  the given UIHierarchyItem
        /// </summary>      
        private static UIHierarchyItem ComparePathsSearch(string testedPath, string searchedPath, UIHierarchyItem item) {
            if (string.Compare(Path.GetFullPath(testedPath), Path.GetFullPath(searchedPath), true) == 0) {
                return item;
            } else {                
                return FindUIHierarchyItem(item.UIHierarchyItems, searchedPath);
            }
        }

        public static string TestSolutionFile {
            get {
                string s = Path.Combine(Path.GetFullPath(@"..\..\..\.."), @"VLUnitTestsContextSolution\VLUnitTestsContextSolution.sln"); 
                return s;
            }
        }

        public static string CSharpStringsTestFile1 {
            get {
                return Path.Combine(Path.GetFullPath(@"..\..\..\.."), @"VLUnitTestsContextSolution\CSharpTests\strings1.cs");
            }
        }

        public static string CSharpStringsTestFormFile1 {
            get {
                return Path.Combine(Path.GetFullPath(@"..\..\..\.."), @"VLUnitTestsContextSolution\CSharpTests\Form1.cs");
            }
        }

        public static string CSharpStringsTestFormDesignerFile1 {
            get {
                return Path.Combine(Path.GetFullPath(@"..\..\..\.."), @"VLUnitTestsContextSolution\CSharpTests\Form1.Designer.cs");
            }
        }

        public static string CSharpStringsTestProject {
            get {
                return Path.Combine(Path.GetFullPath(@"..\..\..\.."), @"VLUnitTestsContextSolution\CSharpTests\CSharpTests.csproj");
            }
        }

        public static string CSharpStringsTestFile2 {
            get {
                return Path.Combine(Path.GetFullPath(@"..\..\..\.."), @"VLUnitTestsContextSolution\CSharpTests\Data\strings2.cs");
            }
        }

        public static string CSharpStringsTestFolder1 {
            get {
                return Path.Combine(Path.GetFullPath(@"..\..\..\.."), @"VLUnitTestsContextSolution\CSharpTests\Data\Inner\");
            }
        }

        public static string CSharpStringsTestFolder2 {
            get {
                return Path.Combine(Path.GetFullPath(@"..\..\..\.."), @"VLUnitTestsContextSolution\CSharpTests\Data\");
            }
        }

        public static string CSharpStringsTestFile3 {
            get {
                return Path.Combine(Path.GetFullPath(@"..\..\..\.."), @"VLUnitTestsContextSolution\CSharpTests\Data\Inner\strings3.cs");
            }
        }        




        public static string VBStringsTestProject {
            get {
                return Path.Combine(Path.GetFullPath(@"..\..\..\.."), @"VLUnitTestsContextSolution\VBTests\VBTests.csproj");
            }
        }

        public static string VBStringsTestFile1 {
            get {
                return Path.Combine(Path.GetFullPath(@"..\..\..\.."), @"VLUnitTestsContextSolution\VBTests\strings1.vb");
            }
        }

        public static string VBStringsTestFile2 {
            get {
                return Path.Combine(Path.GetFullPath(@"..\..\..\.."), @"VLUnitTestsContextSolution\VBTests\Subfolder\strings2.vb");
            }
        }

        public static string VBStringsTestFile3 {
            get {
                return Path.Combine(Path.GetFullPath(@"..\..\..\.."), @"VLUnitTestsContextSolution\VBTests\Subfolder\Inner\strings3.vb");
            }
        }

        public static string VBStringsTestFolder1 {
            get {
                return Path.Combine(Path.GetFullPath(@"..\..\..\.."), @"VLUnitTestsContextSolution\VBTests\Subfolder\");
            }
        }

        public static string VBStringsTestFolder2 {
            get {
                return Path.Combine(Path.GetFullPath(@"..\..\..\.."), @"VLUnitTestsContextSolution\VBTests\Subfolder\Inner\");
            }
        }

        public static string VBStringsTestForm1 {
            get {
                return Path.Combine(Path.GetFullPath(@"..\..\..\.."), @"VLUnitTestsContextSolution\VBTests\Form1.vb");
            }
        }

        public static string VBStringsTestFormDesigner1 {
            get {
                return Path.Combine(Path.GetFullPath(@"..\..\..\.."), @"VLUnitTestsContextSolution\VBTests\Form1.Designer.vb");
            }
        }

        public static string AspNetStringsTestFile1 {
            get {
                return Path.Combine(Path.GetFullPath(@"..\..\..\.."), @"VLUnitTestsContextSolution\WebSite\strings1.aspx");
            }
        }

        public static string AspNetStringsTestFile2 {
            get {
                return Path.Combine(Path.GetFullPath(@"..\..\..\.."), @"VLUnitTestsContextSolution\WebSite\strings2.aspx");
            }
        }

        public static string AspNetStringsCustomAspxFile1 {
            get {
                return Path.Combine(Path.GetFullPath(@"..\..\..\.."), @"VLUnitTestsContextSolution\WebSite\Controls\Custom1.ascx");
            }
        }

        public static string AspNetStringsCustomCsFile1 {
            get {
                return Path.Combine(Path.GetFullPath(@"..\..\..\.."), @"VLUnitTestsContextSolution\WebSite\Controls\Custom1.ascx.cs");
            }
        }

        public static string AspNetStringsCustomAspxFile2 {
            get {
                return Path.Combine(Path.GetFullPath(@"..\..\..\.."), @"VLUnitTestsContextSolution\WebSite\Controls\Custom2.ascx");
            }
        }

        public static string AspNetStringsCustomVbFile2 {
            get {
                return Path.Combine(Path.GetFullPath(@"..\..\..\.."), @"VLUnitTestsContextSolution\WebSite\Controls\Custom2.ascx.vb");
            }
        }

        public static string AspNetStringsTestFolder1 {
            get {
                return Path.Combine(Path.GetFullPath(@"..\..\..\.."), @"VLUnitTestsContextSolution\WebSite\Controls\");
            }
        }

        public static string AspNetStringsTestProject {
            get {
                return Path.Combine(Path.GetFullPath(@"..\..\..\.."), @"VLUnitTestsContextSolution\WebSite\");
            }
        }

        public static string CSharpReferencesTestFile1 {
            get {
                return Path.Combine(Path.GetFullPath(@"..\..\..\.."), @"VLUnitTestsContextSolution\CSharpTests\references1.cs");
            }
        }

        public static string CSharpReferencesTestFile2 {
            get {
                return Path.Combine(Path.GetFullPath(@"..\..\..\.."), @"VLUnitTestsContextSolution\CSharpTests\references2.cs");
            }
        }

        public static string VBReferencesTestFile1 {
            get {
                return Path.Combine(Path.GetFullPath(@"..\..\..\.."), @"VLUnitTestsContextSolution\VBTests\references1.vb");
            }
        }

        public static string AspNetReferencesTestFile1 {
            get {
                return Path.Combine(Path.GetFullPath(@"..\..\..\.."), @"VLUnitTestsContextSolution\WebSite\referencesCS.aspx");
            }
        }

        public static string AspNetReferencesTestFile2 {
            get {
                return Path.Combine(Path.GetFullPath(@"..\..\..\.."), @"VLUnitTestsContextSolution\WebSite\referencesVB.aspx");
            }
        }

        public static string CSharpResourceFile1 {
            get {
                return Path.Combine(Path.GetFullPath(@"..\..\..\.."), @"VLUnitTestsContextSolution\CSharpTests\blank.resx");
            }
        }

        public static string CSharpResourceFileLib {
            get {
                return Path.Combine(Path.GetFullPath(@"..\..\..\.."), @"VLUnitTestsContextSolution\CSharpLib\blank.resx");
            }
        }

        public static string VBResourceFile1 {
            get {
                return Path.Combine(Path.GetFullPath(@"..\..\..\.."), @"VLUnitTestsContextSolution\VBTests\blank.resx");
            }
        }

        public static string VBResourceFileLib {
            get {
                return Path.Combine(Path.GetFullPath(@"..\..\..\.."), @"VLUnitTestsContextSolution\VBLib\blank.resx");
            }
        }

        public static string AspNetResourceFile {
            get {
                return Path.Combine(Path.GetFullPath(@"..\..\..\.."), @"VLUnitTestsContextSolution\WebSite\App_GlobalResources\blank.resx");
            }
        }
    }
}
