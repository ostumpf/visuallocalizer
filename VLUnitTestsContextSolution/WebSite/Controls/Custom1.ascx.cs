using System;
using System.Collections;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Xml.Linq;
using System.ComponentModel;

public partial class Custom1 : System.Web.UI.UserControl {

    protected void Page_Load(object sender, EventArgs e) {
        string a = @"custom1";
        string b = "cus\"t\"om1";
    }

    public string Test1 {
        get;
        set;
    }

    public int Test2 {
        get;
        set;
    }

    [Localizable(false)]
    public string Test3 {
        get;
        set;
    }
}
