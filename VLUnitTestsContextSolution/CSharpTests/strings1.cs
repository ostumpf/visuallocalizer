using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Text;
using System.ComponentModel;

namespace CSharpTests {
    class Program {
        public static void Main(string[] args) {
        }
    }

    abstract class A {
        private string a_54 = @"@";
        private string a_55 = @"@""";
        private string a_56 = @"""@""";
        private string a_57 = @"""""";
        private string a_58 = @"@""@""";

        private string a_1 = "a1";
        private string a_2 = @"\a2\@";
        private string a_3 = @"a3\";
        private string a_4 = @"";
        private string a_5 = "";
        private string a_6 = "\\";
        private string a_7 = "\"";
        private string a_8 = "\"\"";
        private string a_9 = @"""";
        private string a_10 = @"""a10""";
        private string a_11 = @"""""";
        private string a_12 = "a\nb";

        private const string a_13 = "const";
        private readonly string a_14 = "readonly";
        private static string a_15 = "static";

        private string a_16 = @"a
b
c
d

e";
        private string a_17 = @"
";
        private string a_18 = @"f
""
g
""
";
        private char a_19 = 'a';

        public abstract void am_1();
        public virtual void am_2() {
            string a_20 = "a_20";
            "a_21".ToLower();
            new StringBuilder("a_22");
        }

        public static void am_3() {
            // "not a string"
            string a_23 = "a_23";
            //"
            string a_24 = "a_24";
            //
            string a_25 = "a_25"; // @"
            string a_26 = "a_26"; /*
            string a_27 = "a_27";*/
            /*/string a_28 = "a_28";*/
            string a_29 = "a_29";
            string a_30 = "//"; string a_30_ = "a_31";
            string a_31 = "/*";
            string a_32 = "a_32";
            string a_33 = "/*/";
            string a_34 = "a_34";
            /*
            string a_35 = "a_35";
             * /*
             * /*
             * //
            */
            string a_36 = "a_36";
            ///*
            string a_37 = "a_37"; // /*
            string a_38 = "a_38";
        }

        public string am_4 {
            get {
                return "a_39";
            }
            set {
                Console.Write("a_40");
            }
        }

        public string am_5 {
            get {
                return "a_41";
            }
        }

        public string am_6 {
            set {
                Console.Write("a_42");
            }
        }

        class A_c_inner {
            string a_43 = "a_43";
        }

        struct A_s_inner {
            static string a_44 = "a_44";
        }

        [Localizable(false)]
        class A_c1_inner {
            string a_44 = "a_44";

            void m() {
                string a_45 = "a_45";
            }
        }

        [Localizable(false)]
        void m() {
            string a_46 = "a_46";
        }

        void c() {
            string a_47 = "a_47";
            string a_48 = /*VL_NO_LOC*/"a_48";
            string a_49 = "a_49";
        }
    }

    struct B {
        private static string b_1 = "static";

        [Localizable(false)]
        void d() {
            string a_50 = "a_50";
            string a_51 = /*VL_NO_LOC*/"a_51";
            string a_52 = "a_52";
        }
    }

    namespace A_nmspc {
        class X {
            private string a_53 = "a_53";

            void m() {
                string a_59 = @"@";
                string a_60 = @"""@";
                string a_61 = "a_61";
            }
        }
    }
}

class Oc {
    private string a_62 = "a_62";  
}

struct Os {
    private static string a_63 = "a_63";

}