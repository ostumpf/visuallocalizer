Imports System.ComponentModel

Public Class strings2
    Public Function f1() As Integer
        Dim b_1 As String = "b_1"
    End Function

    <Localizable(False)> _
    Public Sub s4()
        Dim b_2 As String = "b_2"
    End Sub

    <Localizable(False)> _
    Public Class C1

        <Localizable(False)> _
        Public Sub C1_s()
            Dim b_3 As String = "b_3"
        End Sub
    End Class

    <Localizable(True)> _
    Public Sub s()
        Dim b_4 As String = "b_4"
    End Sub
End Class
