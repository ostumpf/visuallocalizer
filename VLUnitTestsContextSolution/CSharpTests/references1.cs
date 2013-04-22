using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSharpTests {
    class references1 {
        private string a_1 = Resource1.Key1;
        private string a_2 = Resource1        .          Key1          ;
        private string a_3 = 
            
            Resource1
            
            .
            
            Key1
            
            ;
        private string a_4 = 
Resource1
.
Key1
;
        private string a_5 =Resource1.Key1;
        private string a_6 =Resource1.Key11;

        private void m() {
            string a_7 = Resource1.Key11;
            string a_8 = //Resource1.Key11;
                Resource1
                 . /* test */
                Key1;
            /*
            string a_9 = Resource1.Key11;*/Resource1.Key1.ToLower();
            // Resource1.Key1
            string a_10 = Resource1.Key11;
        }

        private string P_1 {
            get {
                string a_11 = "Resource1.Key1";
                string a_12 = @"
Resource1.Key1
";

                return CSharpTests  .   Resource1.Key1;
            }
            set {
                Console.WriteLine(Resource1.Key1);
                Console.WriteLine(Resource1.Key11);
                Console.WriteLine(Resource1
                    .
                    Key11);
            }
        }

        private string P_2 {
            get {
                return
Resource1              .   /* Resource1.Key11 */                  Key1;
            }           
        }

        private string P_3 {            
            set {
                Console.WriteLine(CSharpTests.Resource1.Key1);
                Console.WriteLine(
CSharpTests
.

Resource1.

Key1);
            }
        }

        private void test(Resource1 Key1) {
        }
    }
}

class Outside {
    private string b_1 = CSharpLib.Resource1.Key1;
    private string b_2 = CSharpTests.Resource1.Key1;
    private string b_3 = CSharpTests.Properties.Resources.Key1;    
}

namespace Inner {
    using CSharpLib;
    class A {
        private string b_1 = CSharpLib.Resource1.Key1;
        private string b_2 = Resource1.Key1;
        private string b_3 = CSharpTests.Properties.Resources.Key1;
    }
}

namespace CSharpTests {
    using al = CSharpLib;
    class D {
        private string b_4 = Properties.Resources.Key1;
        private string b_5 = CSharpLib.Resource1.Key1;
        private string b_6 = al.Resource1.Key1;
    }
}