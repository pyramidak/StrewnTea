Imports System
Imports System.Collections.Generic
Imports System.Globalization
Imports System.Linq
Imports System.Text
Imports System.Windows.Controls

Public Class NumericRule
    Inherits ValidationRule

    Public Overrides Function Validate(value As Object, cultureInfo As CultureInfo) As ValidationResult
        If IsNumeric(value) Then
            Dim input As String = CType(value, String)
            Dim result As Integer
            If Integer.TryParse(input, NumberStyles.Integer, cultureInfo.NumberFormat, result) = False Then
                Return New ValidationResult(False, "Toto není celé číslo!")
            End If
            If CInt(input) < 0 Then
                Return New ValidationResult(False, "Číslo musí být větší než nula!")
            End If
            Return ValidationResult.ValidResult
        Else
            Return New ValidationResult(False, "Toto není číslo!")
        End If
    End Function
End Class



