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
    internal class Agent {
        public static EnvDTE80.DTE2 GetDTE() {
            return (EnvDTE80.DTE2)Package.GetGlobalService(typeof(EnvDTE.DTE));   
        }

        public static EnvDTE.UIHierarchy GetUIHierarchy() {
            return (EnvDTE.UIHierarchy)GetDTE().Windows.Item(EnvDTE.Constants.vsWindowKindSolutionExplorer).Object;
        }

        public static void EnsureSolutionOpen() {
            var DTE = Agent.GetDTE();
            if (DTE.Solution.FileName != TestSolutionFile) {
                DTE.Solution.Open(TestSolutionFile);
            }
        }

        private static BatchMoveCommand _BatchMoveCommand;
        public static BatchMoveCommand BatchMoveCommand {
            get {
                if (_BatchMoveCommand == null) _BatchMoveCommand = new BatchMoveCommand();
                return _BatchMoveCommand;
            }
        }

        private static BatchMoveCommand_Accessor _BatchMoveCommand_Accessor;
        public static BatchMoveCommand_Accessor BatchMoveCommand_Accessor {
            get {
                if (_BatchMoveCommand_Accessor == null) _BatchMoveCommand_Accessor = new BatchMoveCommand_Accessor();
                return _BatchMoveCommand_Accessor;
            }
        }

        private static BatchInlineCommand _BatchInlineCommand;
        public static BatchInlineCommand BatchInlineCommand {
            get {
                if (_BatchInlineCommand == null) _BatchInlineCommand = new BatchInlineCommand();
                return _BatchInlineCommand;
            }
        }

        private static BatchInlineCommand_Accessor _BatchInlineCommand_Accessor;
        public static BatchInlineCommand_Accessor BatchInlineCommand_Accessor {
            get {
                if (_BatchInlineCommand_Accessor == null) _BatchInlineCommand_Accessor = new BatchInlineCommand_Accessor();
                return _BatchInlineCommand_Accessor;
            }
        }

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

        private static UIHierarchyItem ComparePathsSearch(string testedPath, string searchedPath, UIHierarchyItem item) {
            if (string.Compare(Path.GetFullPath(testedPath), Path.GetFullPath(searchedPath), true) == 0) {
                return item;
            } else {                
                return FindUIHierarchyItem(item.UIHierarchyItems, searchedPath);
            }
        }

        public static string TestSolutionFile {
            get {
                return @"C:\Users\Ondra\Documents\Visual Studio 2008\Projects\VLUnitTestsContextSolution\VLUnitTestsContextSolution.sln";
            }
        }

        public static string CSharpStringsTestFile1 {
            get {
                return @"C:\Users\Ondra\Documents\Visual Studio 2008\Projects\VLUnitTestsContextSolution\CSharpTests\strings1.cs";
            }
        }

        public static string CSharpStringsTestFormFile1 {
            get {
                return @"C:\Users\Ondra\Documents\Visual Studio 2008\Projects\VLUnitTestsContextSolution\CSharpTests\Form1.cs";
            }
        }

        public static string CSharpStringsTestFormDesignerFile1 {
            get {
                return @"C:\Users\Ondra\Documents\Visual Studio 2008\Projects\VLUnitTestsContextSolution\CSharpTests\Form1.Designer.cs";
            }
        }

        public static string CSharpStringsTestProject {
            get {
                return @"C:\Users\Ondra\Documents\Visual Studio 2008\Projects\VLUnitTestsContextSolution\CSharpTests\CSharpTests.csproj";
            }
        }

        public static string CSharpStringsTestFile2 {
            get {
                return @"C:\Users\Ondra\Documents\Visual Studio 2008\Projects\VLUnitTestsContextSolution\CSharpTests\Data\strings2.cs";
            }
        }

        public static string CSharpStringsTestFolder1 {
            get {
                return @"C:\Users\Ondra\Documents\Visual Studio 2008\Projects\VLUnitTestsContextSolution\CSharpTests\Data\Inner\";
            }
        }

        public static string CSharpStringsTestFolder2 {
            get {
                return @"C:\Users\Ondra\Documents\Visual Studio 2008\Projects\VLUnitTestsContextSolution\CSharpTests\Data\";
            }
        }

        public static string CSharpStringsTestFile3 {
            get {
                return @"C:\Users\Ondra\Documents\Visual Studio 2008\Projects\VLUnitTestsContextSolution\CSharpTests\Data\Inner\strings3.cs";
            }
        }        




        public static string VBStringsTestProject {
            get {
                return @"C:\Users\Ondra\Documents\Visual Studio 2008\Projects\VLUnitTestsContextSolution\VBTests\VBTests.csproj";
            }
        }

        public static string VBStringsTestFile1 {
            get {
                return @"C:\Users\Ondra\Documents\Visual Studio 2008\Projects\VLUnitTestsContextSolution\VBTests\strings1.vb";
            }
        }

        public static string VBStringsTestFile2 {
            get {
                return @"C:\Users\Ondra\Documents\Visual Studio 2008\Projects\VLUnitTestsContextSolution\VBTests\Subfolder\strings2.vb";
            }
        }

        public static string VBStringsTestFile3 {
            get {
                return @"C:\Users\Ondra\Documents\Visual Studio 2008\Projects\VLUnitTestsContextSolution\VBTests\Subfolder\Inner\strings3.vb";
            }
        }

        public static string VBStringsTestFolder1 {
            get {
                return @"C:\Users\Ondra\Documents\Visual Studio 2008\Projects\VLUnitTestsContextSolution\VBTests\Subfolder\";
            }
        }

        public static string VBStringsTestFolder2 {
            get {
                return @"C:\Users\Ondra\Documents\Visual Studio 2008\Projects\VLUnitTestsContextSolution\VBTests\Subfolder\Inner\";
            }
        }

        public static string VBStringsTestForm1 {
            get {
                return @"C:\Users\Ondra\Documents\Visual Studio 2008\Projects\VLUnitTestsContextSolution\VBTests\Form1.vb";
            }
        }

        public static string VBStringsTestFormDesigner1 {
            get {
                return @"C:\Users\Ondra\Documents\Visual Studio 2008\Projects\VLUnitTestsContextSolution\VBTests\Form1.Designer.vb";
            }
        }

        public static string AspNetStringsTestFile1 {
            get {
                return @"C:\Users\Ondra\Documents\Visual Studio 2008\Projects\VLUnitTestsContextSolution\WebSite\strings1.aspx";
            }
        }

        public static string AspNetStringsTestFile2 {
            get {
                return @"C:\Users\Ondra\Documents\Visual Studio 2008\Projects\VLUnitTestsContextSolution\WebSite\strings2.aspx";
            }
        }

        public static string AspNetStringsCustomAspxFile1 {
            get {
                return @"C:\Users\Ondra\Documents\Visual Studio 2008\Projects\VLUnitTestsContextSolution\WebSite\Controls\Custom1.ascx";
            }
        }

        public static string AspNetStringsCustomCsFile1 {
            get {
                return @"C:\Users\Ondra\Documents\Visual Studio 2008\Projects\VLUnitTestsContextSolution\WebSite\Controls\Custom1.ascx.cs";
            }
        }

        public static string AspNetStringsCustomAspxFile2 {
            get {
                return @"C:\Users\Ondra\Documents\Visual Studio 2008\Projects\VLUnitTestsContextSolution\WebSite\Controls\Custom2.ascx";
            }
        }

        public static string AspNetStringsCustomVbFile2 {
            get {
                return @"C:\Users\Ondra\Documents\Visual Studio 2008\Projects\VLUnitTestsContextSolution\WebSite\Controls\Custom2.ascx.vb";
            }
        }

        public static string AspNetStringsTestFolder1 {
            get {
                return @"C:\Users\Ondra\Documents\Visual Studio 2008\Projects\VLUnitTestsContextSolution\WebSite\Controls\";
            }
        }

        public static string AspNetStringsTestProject {
            get {
                return @"C:\Users\Ondra\Documents\Visual Studio 2008\Projects\VLUnitTestsContextSolution\WebSite\";
            }
        }

        public static string CSharpReferencesTestFile1 {
            get {
                return @"C:\Users\Ondra\Documents\Visual Studio 2008\Projects\VLUnitTestsContextSolution\CSharpTests\references1.cs";
            }
        }

        public static string CSharpReferencesTestFile2 {
            get {
                return @"C:\Users\Ondra\Documents\Visual Studio 2008\Projects\VLUnitTestsContextSolution\CSharpTests\references2.cs";
            }
        }

        public static string VBReferencesTestFile1 {
            get {
                return @"C:\Users\Ondra\Documents\Visual Studio 2008\Projects\VLUnitTestsContextSolution\VBTests\references1.vb";
            }
        }

        public static string AspNetReferencesTestFile1 {
            get {
                return @"C:\Users\Ondra\Documents\Visual Studio 2008\Projects\VLUnitTestsContextSolution\WebSite\referencesCS.aspx";
            }
        }

        public static string AspNetReferencesTestFile2 {
            get {
                return @"C:\Users\Ondra\Documents\Visual Studio 2008\Projects\VLUnitTestsContextSolution\WebSite\referencesVB.aspx";
            }
        }
    }
}
