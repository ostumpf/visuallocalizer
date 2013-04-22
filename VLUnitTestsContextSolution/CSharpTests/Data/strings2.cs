using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSharpTests.Data {
    class strings2 {
        class A {
            class B {
                string test3 = /*@*/"test3";
                string test4 = "test4";
                string test5 = "Hello,"+   " World!";
                string test6 = @"first"+
                    
                     "second";
                string test7 = "first"+
                    Environment.NewLine+
                    "second";
            }
        }
    }
}
