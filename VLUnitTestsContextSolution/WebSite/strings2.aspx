<%@ Page Language="VB" AutoEventWireup="false" CodeFile="strings2.aspx.vb" Inherits="strings2" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Untitled Page</title>
</head>
<body>
    <form ID="form1" runat="server">
        <%="some""string"""%>
        <%
            Dim a As String = "test"
            Dim b As String = "test""test"
            Dim c As Char = "r"c
            ' "nothing"
            REM "a_2"
            Dim e As String = "ee"
            %>
        <script runat="server">
            Dim x As String = "x23"
        </script>
        <%Dim z As String = "40"%>
        <script runat="server">Dim x1 As String = "ttt"</script>
        <script runat="server">Dim x2 As String = ""</script>
    </form>
</body>
</html>
