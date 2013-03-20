<%@ Page Language="C#" AutoEventWireup="true" CodeFile="EditPeople.aspx.cs" Inherits="EditPeople" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head id="Head1" runat="server">
    <title>Edit People</title>
</head>
<body>
       
    <asp:LinqDataSource ID="LinqDataSource1" runat="server" 
        ContextTypeName="SchoolDataContext" EnableUpdate="True" EntityTypeName="" 
        TableName="Persons">
    </asp:LinqDataSource>
    <asp:FormView ID="FormView1" runat="server" AllowPaging="True" 
        DataKeyNames="PersonID" DataSourceID="LinqDataSource1">
        <EditItemTemplate>
            PersonID:
            <asp:Label ID="PersonIDLabel1" runat="server" Text='<%# Eval("PersonID") %>' />
            <br />
            LastName:
            <asp:TextBox ID="LastNameTextBox" runat="server" 
                Text='<%# Bind("LastName") %>' />
            <br />                
           
            <asp:LinkButton ID="UpdateButton" runat="server" CausesValidation="True" 
                CommandName="Update" Text='/>' AccessKey="asdsad" BackColor="44" />
            &nbsp;<asp:LinkButton ID="UpdateCancelButton" runat="server" 
                CausesValidation="False" CommandName="Cancel" Text="<%-- visible --%>Cancel" />
                TEST1<!-- <asp:Text ID="Text1"/> -->TEST2
            <!--<asp:TextBox ID="Text2"></asp:TextBox>-->
                <%-- <asp:Button Text="Not displayed"/> not reported <!-- not reported 2 --> --%>
        </EditItemTemplate>                      
    </asp:FormView>
    <asp:TextBox ID="<%= "TEST3" %>"/>
    <asp:TextBox ID="<%: "TEST4" %>"/>
    <%= "TEST5" %>
    <% string s="come code"; %>    
    <% string s="<!--"; %>
    <% string s="<%"; %>
</body>
</html>
