Public Class A
    Private a_1 As String = "a_1"
    Private a_2 As String = ""
    Private a_3 As String = """a_3"""
    Private a_4 As String = """"
    Private a_5 As String = """"""
    Private a_6 As String = """\"""
    Private a_7 As String = "@"

    Private a_8 As Char = "a"c

    Private Sub s_1()
        Dim a_9 As String = "a_9"
        Dim a_10 As String = "a_10"
    End Sub

    Private Function f_1() As String
        '"a_11" 
        Dim a_12 As String = "a_12"
        '"
        Dim a_13 As String = "a_13"
        '' aaa "bbb"
        Dim a_14 As String = "a_14"
        Dim a_15 As String = "'"
        Dim a_16 As String = "a_16"
        Dim a_17 As String = "''"
        Dim a_18 As String = "a_18"

        REM "test"
        Dim a_19 As String = "a_19"
        REM aaaa "bbb"
        'REM "cc"
        REM ee ' "dd"
        Dim a_20 As String = "REM"
        Dim a_REM_b As String = "a_21"
        Dim a_REM As String = "a_22"
        Dim REM_b As String = "a_23"
        Dim _REM As String = "a_24"
        Dim REM_ As String = "a_25"

        Return ""
    End Function

    Private Class A_c_inner
        Dim a_26 As String = "a_26"
    End Class

    Private Structure A_s_inner
        Shared a_27 As String = "a_27"
        Dim x As Integer
    End Structure

End Class

Module M
    Class R
        ReadOnly a_28 As String = "a_28"
        Dim a_29 As String = "a_29"
        Const a_30 As String = "a_30"
    End Class

    Dim a_31 As String = "a_31"
End Module

Namespace N1
    Module M1
        Dim a_32 As String = "a_32"
    End Module

    Namespace N1_inner
        Class C_inner
            Dim a_33 As String = "a_33"
        End Class

        Class C_inner_2
            Dim a_34 As String = "a_34"+ControlChars.NewLine &ControlChars.Quote &"@baf"
            Dim a_35 As String = "a_35" & "baf"
            Dim a_36 As String = "a_36"+"baf"
            Dim a_37 As String = "a_37" _
    + _
             "baf"
            Dim a_38 As String = "a_38" & vbNewLine + "baf"
            Dim a_39 As String = "a_39" & ControlChars.Quote+"baf"
            Dim a_40 As String = "a_40" &Chr(64)+"baf"
            Dim a_41 As String = "a_41" & _
                vbNewLine + _
            "baf"
            Dim a_42 As String = "a_42" & _
Microsoft.VisualBasic.ControlChars.Quote & "baf"
            Dim a_43 As String = "a_43" & _
Microsoft. VisualBasic.Chr(64) + "baf"
        End Class
    End Namespace


End Namespace




