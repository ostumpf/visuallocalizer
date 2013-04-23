using VisualLocalizer.Library.AspxParser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

namespace VLUnitTests {

    /// <summary>
    /// Tests for AspX parser.
    /// </summary>
    [TestClass()]
    public class ParserTest {

        private TestContext testContextInstance;

        public TestContext TestContext {
            get {
                return testContextInstance;
            }
            set {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion

        /// <summary>
        /// Tests if the parser reports all elements, attributes etc. with correctly initialized data.
        /// </summary>
        [TestMethod()]        
        public void ProcessTest() {
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("VLUnitTests.Resources.AspxTest.aspx");
            string text = stream.readAll(); // read the aspx text
            
            TestAspxHandler handler = new TestAspxHandler(); // register testing handler
            Parser parser = new Parser(text, handler);
            
            parser.Process();
        }
    }

    /// <summary>
    /// Testing handler of AspX parser events - each event is checked whether it occured in the right time and whether it contains the right data.
    /// </summary>
    public class TestAspxHandler : IAspxHandler {

        /// <summary>
        /// Current event number 
        /// </summary>
        private int index = 0;

        /// <summary>
        /// Compares expected and actual list of attributes
        /// </summary>        
        private void CheckAttrs(List<AttributeInfo> a, List<AttributeInfo> b) {
            for (int i = 0; i < a.Count; i++) {
                Assert.AreEqual(a[i].Name, b[i].Name);
                Assert.AreEqual(a[i].ContainsAspTags, b[i].ContainsAspTags);
                Assert.AreEqual(a[i].Value, b[i].Value);               
            }            
        }

        public bool StopRequested {
            get { return false; }
        }

        public void OnCodeBlock(CodeBlockContext context) {          
            bool matched = false;
            if (index == 36) {
                matched = true;
                Assert.IsFalse(context.WithinClientSideComment);
                Assert.AreEqual(context.BlockText.Trim(), "string s=\"come code\";");
            }
            if (index == 37) {
                matched = true;
                Assert.IsFalse(context.WithinClientSideComment);
                Assert.AreEqual(context.BlockText.Trim(), "string s=\"<!--\";");
            }
            if (index == 38) {
                matched = true;
                Assert.IsFalse(context.WithinClientSideComment);
                Assert.AreEqual(context.BlockText.Trim(), "string s=\"<%\";");
            }
            if (index == 39) {
                matched = true;
                Assert.IsFalse(context.WithinClientSideComment);
                Assert.AreEqual(context.BlockText.Trim(), "string s = \"baf\";");
            }
            Assert.IsTrue(matched);
            index++;
        }

        public void OnPageDirective(DirectiveContext context) {
            Assert.AreEqual(0, index);
            Assert.AreEqual(4, context.Attributes.Count);
            Assert.AreEqual("Page", context.DirectiveName);            

            List<AttributeInfo> validAttrs=new List<AttributeInfo>();
            validAttrs.Add(new AttributeInfo() { Name = "Language", Value = "C#", ContainsAspTags = false });
            validAttrs.Add(new AttributeInfo() { Name = "AutoEventWireup", Value = "true", ContainsAspTags = false });
            validAttrs.Add(new AttributeInfo() { Name = "CodeFile", Value = "EditPeople.aspx.cs", ContainsAspTags = false });
            validAttrs.Add(new AttributeInfo() { Name = "Inherits", Value = "EditPeople", ContainsAspTags = false });

            CheckAttrs(validAttrs, context.Attributes);
            
            index++;
        }

        public void OnOutputElement(OutputElementContext context) {
            bool matched = false;
            if (index == 14) {
                matched = true;
                Assert.IsTrue(context.Kind == OutputElementKind.BIND);
                Assert.IsTrue(context.WithinElementsAttribute);
                Assert.IsFalse(context.WithinClientSideComment);
                Assert.AreEqual(context.InnerText.Trim(), "Eval(\"PersonID\")");
            }
            if (index == 18) {
                matched = true;
                Assert.IsTrue(context.Kind == OutputElementKind.BIND);
                Assert.IsTrue(context.WithinElementsAttribute);
                Assert.IsFalse(context.WithinClientSideComment);                
                Assert.AreEqual(context.InnerText.Trim(), "Bind(\"LastName\")");
            }
            if (index == 31) {
                matched = true;
                Assert.IsTrue(context.Kind == OutputElementKind.PLAIN);
                Assert.IsTrue(context.WithinElementsAttribute);
                Assert.IsFalse(context.WithinClientSideComment);
                Assert.AreEqual(context.InnerText.Trim(), "\"TEST3\"");
            }
            if (index == 33) {
                matched = true;
                Assert.IsTrue(context.Kind == OutputElementKind.HTML_ESCAPED);
                Assert.IsTrue(context.WithinElementsAttribute);
                Assert.IsFalse(context.WithinClientSideComment);
                Assert.AreEqual(context.InnerText.Trim(), "\"TEST4\"");
            }
            if (index == 35) {
                matched = true;
                Assert.IsTrue(context.Kind == OutputElementKind.PLAIN);
                Assert.IsFalse(context.WithinElementsAttribute);
                Assert.IsFalse(context.WithinClientSideComment);
                Assert.AreEqual(context.InnerText.Trim(), "\"TEST5\"");
            }
            Assert.IsTrue(matched);
            index++;
        }

        public void OnElementBegin(ElementContext context) {
            bool matched = false;
            if (index == 1) {
                matched = true;
                Assert.AreEqual("!DOCTYPE", context.ElementName);
                Assert.AreEqual(2, context.Attributes.Count);
                Assert.IsFalse(context.WithinClientSideComment);
                
                List<AttributeInfo> validAttrs = new List<AttributeInfo>();
                validAttrs.Add(new AttributeInfo() { Name = "PUBLIC", Value = "-//W3C//DTD XHTML 1.0 Transitional//EN", ContainsAspTags = false });
                validAttrs.Add(new AttributeInfo() { Name = "", Value = "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd", ContainsAspTags = false });

                CheckAttrs(validAttrs, context.Attributes);                
            }
            if (index == 2) {
                matched = true;
                Assert.AreEqual("html", context.ElementName);
                Assert.AreEqual(1, context.Attributes.Count);
                Assert.IsFalse(context.WithinClientSideComment);

                List<AttributeInfo> validAttrs = new List<AttributeInfo>();
                validAttrs.Add(new AttributeInfo() { Name = "xmlns", Value = "http://www.w3.org/1999/xhtml", ContainsAspTags = false });

                CheckAttrs(validAttrs, context.Attributes);                
            }
            if (index == 3) {
                matched = true;
                Assert.AreEqual("head", context.ElementName);
                Assert.AreEqual(2, context.Attributes.Count);
                Assert.IsFalse(context.WithinClientSideComment);

                List<AttributeInfo> validAttrs = new List<AttributeInfo>();
                validAttrs.Add(new AttributeInfo() { Name = "id", Value = "Head1", ContainsAspTags = false });
                validAttrs.Add(new AttributeInfo() { Name = "runat", Value = "server", ContainsAspTags = false });

                CheckAttrs(validAttrs, context.Attributes);                
            }
            if (index == 4) {
                matched = true;
                Assert.AreEqual("title", context.ElementName);
                Assert.AreEqual(0, context.Attributes.Count);
                Assert.IsFalse(context.WithinClientSideComment);
            }
            if (index == 8) {
                matched = true;
                Assert.AreEqual(null, context.Prefix);
                Assert.AreEqual("body", context.ElementName);
                Assert.IsFalse(context.WithinClientSideComment);
            }
            if (index == 9) {
                matched = true;
                Assert.AreEqual("LinqDataSource", context.ElementName);
                Assert.AreEqual("asp", context.Prefix);
                Assert.AreEqual(6, context.Attributes.Count);
                Assert.IsFalse(context.WithinClientSideComment);

                List<AttributeInfo> validAttrs = new List<AttributeInfo>();
                validAttrs.Add(new AttributeInfo() { Name = "ID", Value = "LinqDataSource1", ContainsAspTags = false });
                validAttrs.Add(new AttributeInfo() { Name = "runat", Value = "server", ContainsAspTags = false });
                validAttrs.Add(new AttributeInfo() { Name = "ContextTypeName", Value = "SchoolDataContext", ContainsAspTags = false });
                validAttrs.Add(new AttributeInfo() { Name = "EnableUpdate", Value = "True", ContainsAspTags = false });
                validAttrs.Add(new AttributeInfo() { Name = "EntityTypeName", Value = "", ContainsAspTags = false });
                validAttrs.Add(new AttributeInfo() { Name = "TableName", Value = "Persons", ContainsAspTags = false });

                CheckAttrs(validAttrs, context.Attributes);                
            }
            if (index == 11) {
                matched = true;
                Assert.AreEqual("FormView", context.ElementName);
                Assert.AreEqual("asp", context.Prefix);
                Assert.AreEqual(5, context.Attributes.Count);
                Assert.IsFalse(context.WithinClientSideComment);

                List<AttributeInfo> validAttrs = new List<AttributeInfo>();
                validAttrs.Add(new AttributeInfo() { Name = "ID", Value = "FormView1", ContainsAspTags = false });
                validAttrs.Add(new AttributeInfo() { Name = "runat", Value = "server", ContainsAspTags = false });
                validAttrs.Add(new AttributeInfo() { Name = "AllowPaging", Value = "True", ContainsAspTags = false });
                validAttrs.Add(new AttributeInfo() { Name = "DataKeyNames", Value = "PersonID", ContainsAspTags = false });
                validAttrs.Add(new AttributeInfo() { Name = "DataSourceID", Value = "LinqDataSource1", ContainsAspTags = false });
                
                CheckAttrs(validAttrs, context.Attributes);                
            }
            if (index == 12) {
                matched = true;
                Assert.AreEqual("EditItemTemplate", context.ElementName);
                Assert.IsTrue(string.IsNullOrEmpty(context.Prefix));
                Assert.AreEqual(0, context.Attributes.Count);
                Assert.IsFalse(context.WithinClientSideComment);
            }
            if (index == 15) {
                matched = true;
                Assert.AreEqual("Label", context.ElementName);
                Assert.AreEqual("asp", context.Prefix);
                Assert.AreEqual(3, context.Attributes.Count);
                Assert.IsFalse(context.WithinClientSideComment);

                List<AttributeInfo> validAttrs = new List<AttributeInfo>();
                validAttrs.Add(new AttributeInfo() { Name = "ID", Value = "PersonIDLabel1", ContainsAspTags = false });
                validAttrs.Add(new AttributeInfo() { Name = "runat", Value = "server", ContainsAspTags = false });
                validAttrs.Add(new AttributeInfo() { Name = "Text", Value = "<%# Eval(\"PersonID\") %>", ContainsAspTags = true });
                
                CheckAttrs(validAttrs, context.Attributes);
            }
            if (index == 16) {
                matched = true;
                Assert.AreEqual(null, context.Prefix);
                Assert.AreEqual("br", context.ElementName);
                Assert.IsFalse(context.WithinClientSideComment);
            }
            if (index == 19) {
                matched = true;
                Assert.AreEqual("TextBox", context.ElementName);
                Assert.AreEqual("asp", context.Prefix);
                Assert.AreEqual(3, context.Attributes.Count);
                Assert.IsFalse(context.WithinClientSideComment);

                List<AttributeInfo> validAttrs = new List<AttributeInfo>();
                validAttrs.Add(new AttributeInfo() { Name = "ID", Value = "LastNameTextBox", ContainsAspTags = false });
                validAttrs.Add(new AttributeInfo() { Name = "runat", Value = "server", ContainsAspTags = false });
                validAttrs.Add(new AttributeInfo() { Name = "Text", Value = "<%# Bind(\"LastName\") %>", ContainsAspTags = true });

                CheckAttrs(validAttrs, context.Attributes);                
            }
            if (index == 20) {
                matched = true;
                Assert.AreEqual(null, context.Prefix);
                Assert.AreEqual("br", context.ElementName);
                Assert.IsFalse(context.WithinClientSideComment);
            }
            if (index == 21) {
                matched = true;
                Assert.AreEqual("LinkButton", context.ElementName);
                Assert.AreEqual("asp", context.Prefix);
                Assert.AreEqual(7, context.Attributes.Count);
                Assert.IsFalse(context.WithinClientSideComment);

                List<AttributeInfo> validAttrs = new List<AttributeInfo>();
                validAttrs.Add(new AttributeInfo() { Name = "ID", Value = "UpdateButton", ContainsAspTags = false });
                validAttrs.Add(new AttributeInfo() { Name = "runat", Value = "server", ContainsAspTags = false });
                validAttrs.Add(new AttributeInfo() { Name = "CausesValidation", Value = "True", ContainsAspTags = false });
                validAttrs.Add(new AttributeInfo() { Name = "CommandName", Value = "Update", ContainsAspTags = false });
                validAttrs.Add(new AttributeInfo() { Name = "Text", Value = "/>", ContainsAspTags = false });
                validAttrs.Add(new AttributeInfo() { Name = "AccessKey", Value = "asdsad", ContainsAspTags = false });
                validAttrs.Add(new AttributeInfo() { Name = "BackColor", Value = "44", ContainsAspTags = false });

                CheckAttrs(validAttrs, context.Attributes);                
            }
            if (index == 23) {
                matched = true;
                Assert.AreEqual("LinkButton", context.ElementName);
                Assert.AreEqual("asp", context.Prefix);
                Assert.AreEqual(5, context.Attributes.Count);
                Assert.IsFalse(context.WithinClientSideComment);

                List<AttributeInfo> validAttrs = new List<AttributeInfo>();
                validAttrs.Add(new AttributeInfo() { Name = "ID", Value = "UpdateCancelButton", ContainsAspTags = false });
                validAttrs.Add(new AttributeInfo() { Name = "runat", Value = "server", ContainsAspTags = false });
                validAttrs.Add(new AttributeInfo() { Name = "CausesValidation", Value = "False", ContainsAspTags = false });
                validAttrs.Add(new AttributeInfo() { Name = "CommandName", Value = "Cancel", ContainsAspTags = false });
                validAttrs.Add(new AttributeInfo() { Name = "Text", Value = "<%-- visible --%>Cancel", ContainsAspTags = false });

                CheckAttrs(validAttrs, context.Attributes);                
            }
            if (index == 25) {
                matched = true;
                Assert.AreEqual("Text", context.ElementName);
                Assert.AreEqual("asp", context.Prefix);
                Assert.AreEqual(1, context.Attributes.Count);
                Assert.IsTrue(context.WithinClientSideComment);

                List<AttributeInfo> validAttrs = new List<AttributeInfo>();
                validAttrs.Add(new AttributeInfo() { Name = "ID", Value = "Text1", ContainsAspTags = false });
                
                CheckAttrs(validAttrs, context.Attributes);                
            }
            if (index == 27) {
                matched = true;
                Assert.AreEqual("TextBox", context.ElementName);
                Assert.AreEqual("asp", context.Prefix);
                Assert.AreEqual(1, context.Attributes.Count);
                Assert.IsTrue(context.WithinClientSideComment);

                List<AttributeInfo> validAttrs = new List<AttributeInfo>();
                validAttrs.Add(new AttributeInfo() { Name = "ID", Value = "Text2", ContainsAspTags = false });

                CheckAttrs(validAttrs, context.Attributes);                
            }
            if (index == 32) {
                matched = true;
                Assert.AreEqual("TextBox", context.ElementName);
                Assert.AreEqual("asp", context.Prefix);
                Assert.AreEqual(1, context.Attributes.Count);
                Assert.IsFalse(context.WithinClientSideComment);

                List<AttributeInfo> validAttrs = new List<AttributeInfo>();
                validAttrs.Add(new AttributeInfo() { Name = "ID", Value = "<%= \"TEST3\" %>", ContainsAspTags = true });

                CheckAttrs(validAttrs, context.Attributes);
            }
            if (index == 34) {
                matched = true;
                Assert.AreEqual("TextBox", context.ElementName);
                Assert.AreEqual("asp", context.Prefix);
                Assert.AreEqual(1, context.Attributes.Count);
                Assert.IsFalse(context.WithinClientSideComment);

                List<AttributeInfo> validAttrs = new List<AttributeInfo>();
                validAttrs.Add(new AttributeInfo() { Name = "ID", Value = "<%: \"TEST4\" %>", ContainsAspTags = true });

                CheckAttrs(validAttrs, context.Attributes);
            }
            Assert.IsTrue(matched);
            index++;
        }

        public void OnElementEnd(EndElementContext context) {
            bool matched = false;
            if (index == 6) {
                matched = true;
                Assert.IsFalse(context.WithinClientSideComment);
                Assert.IsTrue(string.IsNullOrEmpty(context.Prefix));
                Assert.AreEqual("title", context.ElementName);
            }
            if (index == 7) {
                matched = true;
                Assert.IsFalse(context.WithinClientSideComment);
                Assert.IsTrue(string.IsNullOrEmpty(context.Prefix));
                Assert.AreEqual("head", context.ElementName);
            }
            if (index == 10) {
                matched = true;
                Assert.AreEqual("asp", context.Prefix);
                Assert.IsFalse(context.WithinClientSideComment);                
                Assert.AreEqual("LinqDataSource", context.ElementName);
            }
            if (index == 29) {
                matched = true;
                Assert.IsFalse(context.WithinClientSideComment);
                Assert.IsTrue(string.IsNullOrEmpty(context.Prefix));
                Assert.AreEqual("EditItemTemplate", context.ElementName);
            }
            if (index == 30) {
                matched = true;
                Assert.AreEqual("asp", context.Prefix);
                Assert.IsFalse(context.WithinClientSideComment);
                Assert.AreEqual("FormView", context.ElementName);
            }
            if (index == 40) {
                matched = true;
                Assert.IsFalse(context.WithinClientSideComment);
                Assert.IsTrue(string.IsNullOrEmpty(context.Prefix));
                Assert.AreEqual("body", context.ElementName);
            }
            if (index == 41) {
                matched = true;
                Assert.IsFalse(context.WithinClientSideComment);
                Assert.IsTrue(string.IsNullOrEmpty(context.Prefix));
                Assert.AreEqual("html", context.ElementName);
            }
            if (index == 28) {
                matched = true;
                Assert.IsTrue(context.WithinClientSideComment);
                Assert.AreEqual("asp", context.Prefix);
                Assert.AreEqual("TextBox", context.ElementName);
            }
            Assert.IsTrue(matched);
            index++;
        }

        public void OnPlainText(PlainTextContext context) {
            bool matched = false;
            if (index == 5) {
                matched = true;
                Assert.IsFalse(context.WithinClientSideComment);
                Assert.AreEqual(context.Text.Trim(), "Edit People");
            }
            if (index == 13) {
                matched = true;
                Assert.IsFalse(context.WithinClientSideComment);
                Assert.AreEqual(context.Text.Trim(), "PersonID:");
            }
            if (index == 17) {
                matched = true;
                Assert.IsFalse(context.WithinClientSideComment);
                Assert.AreEqual(context.Text.Trim(), "LastName:");
            }
            if (index == 22) {
                matched = true;
                Assert.IsFalse(context.WithinClientSideComment);
                Assert.AreEqual(context.Text.Trim(), "&nbsp;");
            }
            if (index == 24) {
                matched = true;
                Assert.IsFalse(context.WithinClientSideComment);
                Assert.AreEqual(context.Text.Trim(), "TEST1");
            }
            if (index == 26) {
                matched = true;
                Assert.IsFalse(context.WithinClientSideComment);
                Assert.AreEqual(context.Text.Trim(), "TEST2");
            }
            Assert.IsTrue(matched);
            index++;
        }

        
    }
}
