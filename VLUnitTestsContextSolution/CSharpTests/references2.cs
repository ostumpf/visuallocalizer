using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using test = CSharpTests;

namespace CSharpTests {
    class references2 {
        void m() {
            string a2 = Resource1.Key1;
            string a3 = Resource1.Key11;

            string a4 = test.Resource1.Key1;
            string a5 = test.Resource1.Key11;

            string a6 = CSharpTests.Resource1.Key1;
        }
    }

    namespace Inner {        
        using Properties;

        struct S {

            void m() {
                string a1 = Resource1.Key1;
                string a7 = test.Resource1.Key1;
                string a8 = test.Resource1.Key11;

                string a9 = CSharpLib.Resource1.Key1;
                string a10 = Properties.Resources.Key1;
                string a11 = Properties.Resources.Key2;
                string a13 = Properties.Resources.Key2;
                string a14 = Properties.Resources.Key4;
                string a15 = Properties.Resources.Key5;
                string a16 = Properties.Resources.Key6;

                string a17 = CSharpTests.Properties.Resources.Key1;
            }

        }

    }
}
