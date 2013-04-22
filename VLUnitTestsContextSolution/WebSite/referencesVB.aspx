<%@ Page Language="VB" AutoEventWireup="false" CodeFile="referencesVB.aspx.vb" Inherits="referencesVB" %>

<%@ Import Namespace="Resources" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Untitled Page</title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <asp:Button runat="server" ID="button1" Text="<%$ Resources:Resource1,Key1 %>" />
        <asp:Button runat="server" ID="button2" Text="
    <%$ 
        Resources
        :
        Resource1
,
Key1
      %>" />
   <p><%= Resource1.Key1 %></p>
   <p><%= Resources.Resource1.Key1 %></p>
   <p><%= Resource2. _
      Key1 %></p>
   <p><%= Resource1 _
      . _
      Key1 
          %></p>
      
      <% Dim a As String = Resource1 _
      . _
      Key1 
         
         Dim b As String = Resource2 _
      . _
      Key1  
          
         Dim c As String = _
         Resources _
         .Resource2 _
      . _
      Key1   
      %>
    </div>
    </form>
    
</body>
</html>
