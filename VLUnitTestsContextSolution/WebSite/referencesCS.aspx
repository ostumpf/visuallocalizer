<%@ Page Language="C#" AutoEventWireup="true" CodeFile="referencesCS.aspx.cs" Inherits="referencesCS" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Untitled Page</title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <asp:Button runat="server" ID="button1" Text="<%$ Resources:Resource1,Key1 %>" />
        <asp:Button runat="server" ID="button2" Text="<%$ Resources
        :
        Resource1
        ,
Key1 %>" />
        <asp:Button runat="server" ID="button3" Text="<%$ Resources:Resource1
,
Key11 %>" />
        <asp:Button runat="server" ID="button4" Text="<%$ Resources:Resource1,
Key11 %>" />
        <asp:Button runat="server" ID="button5" Text="<%$ 
Resources:Resource1,Key1 
        %>" />
        
     <p><%= Resources.Resource1.Key1 %></p>
     <p><%= Resources
            .
                    Resource1
                            .
                                    
                                    Key1 %></p>
     <p><%= 
            Resources.Resource1.Key1 
            %></p>                                    
        <% string a_1 = Resources.Resource1.Key1; %>     
        <% 
            string a_2 = Resources.Resource1.Key1;
            string a_3 = Resources.Resource1.Key11;
            string a_4 = Resources.Resource1.Key2;
            string a_5 = Resources.Resource1.Key1;
            string a_6 = 
                Resources
.
Resource1
.
Key1;
            string a_7 = Resources.Resource2.Key1;
            //string a_7 = Resources.Resource2.Key11;
        %>     
        <%-- <asp:Button runat="server" ID="button1" Text="<%$ Resources:Resource1,Key1 %>" /> --%>
        <!-- <asp:Button runat="server" ID="buttonX" Text="<%$ Resources:Resource1,Key11 %>" /> -->
        <asp:Button runat="server" ID="button8" Text="<%$ Resources:Resource1,Key11 %>" /> 
        <asp:Button runat="server" ID="button6" Text="<%$ Resources:Resource2,Key1 %>" /> 
    </div>
    </form>
</body>
</html>
