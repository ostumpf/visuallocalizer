<%@ Page Language="C#" AutoEventWireup="True" CodeFile="strings1.aspx.cs" Inherits="strings1" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Untitled Page</title>
</head>
<body>
    <form ID="form1" runat="server">
        <my:MyElement1 runat="server" ID="mytest" Test1="test1value" Test2="5" Test3="test3value"/>
        <my:MyElement2 runat="server" ID="mytes2" Test1="test1value" Test2="5" Test3="test3value"/>
        
        <asp:Button runat="server" ID="button1" Text="button1_text" BackColor="Aqua" Enabled="true" Font-Size="17" />
        <asp:Button runat="server" ID="button2" Text="&lt;"/>
        <%
            string asp1 = "asp1";
            string asp2 = @"asp2""asp2";
             %>
         <asp:Literal Text='<%= "lit_1" %>'></asp:Literal>             
         <asp:Literal Text="&quot;"></asp:Literal>    
         <asp:Literal Text='<%# Eval("Name") %>'></asp:Literal>    
         <asp:Literal Text='t"a"v'></asp:Literal>    
         
         PlainText
         <%= "<!--" %>
         <asp:Literal Text='<!--'></asp:Literal><p>PlainText2</p><asp:Literal Text="@"></asp:Literal>                     
         <script runat="server">string a = "scr";</script>
         <script type="text/javascript" language="javascript">window.alert("javascript")</script>         
         <%= "output" %>
         <script runat="server">             
             string b = "test2";  
         </script>   
         <% string s = "aaa"; %>
    </form>
</body>
</html>
