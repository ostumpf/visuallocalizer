Imports VBTests.My
Imports CSharpLib

Public Class references1
    Private a_1 As String = VBTests.My.Resources.Resources.Key1
    Private a_2 As String = My.Resources.Key1
    Private a_3 As String = Resources.Key1

    Private Sub m()
        Dim b_1 As String = My      .              Resources.     Key1
        Dim b_2 As String = VBTests   .             My.Resources        .Key1
        'Dim b_3 As String = My.Resources.Key1
        REM Dim b_4 As String = Resources.Key1
        Dim b_5 As String = My      .              Resources.   _REM
        Dim b_6 As String = Resources._REM
        Dim b_7 As String = Resources.REM_
    End Sub

    Private Sub n()
        Dim c_1 As String = "REM" 
        Dim c_2 As String = Resources.Key1         REM Dim c_3 As String = Resources.Key1
    End Sub

    Private Sub o()
        Dim d_1 As String = CSharpLib.Resource1.Key1
        Dim d_2 As String = VBLib.My.Resources.Resource1.Key1
        Dim d_3 As String = My.Resources.Resource1.Key1
        Dim d_4 As String = Resources.Resource1.Key1        
    End Sub
End Class

Namespace test
    
    Class X 
        Sub m()
            Dim e_1 As String = VBTests.My.Resources.Key1            
            Dim e_3 As String = Resource1.Key1
            Dim e_4 As String = VBTests.My.Resources.Resource1.Key2
            Dim e_5 As String = VBTests.My.Resources.Resource1.Key3

            Dim e_6 As String = VBTests _
. _
My.Resources.Resource1.Key1

            Dim e_7 As String = _
            VBTests _
 _
. _
My.Resources _
.Resource1.Key1

            Dim e_8 As String = My.Resources. _
            _REM

            Dim e_9 As String = VBTests.My.Resources _
.Resource1 _
. _
Key1
            Dim e_10 As String = CustomNamespace.Resource2.KeyA
            Dim e_11 As String = VBLib.CustomVB.Resource2.KeyB
            Dim e_12 As String = VBLib.Resource3.KeyC
        End Sub
    End Class
End Namespace

Namespace CustomVB
    Class X
        Dim e_13 As String = VBLib.CustomVB.Resource2.KeyB
        Dim e_14 As String = Resource1.Key1
    End Class
End Namespace