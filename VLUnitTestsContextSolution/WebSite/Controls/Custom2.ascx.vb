Imports System.ComponentModel

Partial Class Controls_Custom2
    Inherits System.Web.UI.UserControl

    Public Property Test1() As String
        Get
            Return "test1"
        End Get
        Set(ByVal value As String)

        End Set
    End Property

    Public Property Test2() As Integer
        Get
            Return 5
        End Get
        Set(ByVal value As Integer)

        End Set
    End Property

    <Localizable(False)> _
    Public Property Test3() As String
        Get
            Return "test3"
        End Get
        Set(ByVal value As String)

        End Set
    End Property

End Class
