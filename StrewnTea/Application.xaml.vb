Imports System.Windows.Threading

Class Application

    Public Shared StartUpLocation As String = myFolder.Path(System.Reflection.Assembly.GetExecutingAssembly().Location)
    Public Shared VersionNo As Integer = CInt(System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly.Location).FileMajorPart & System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly.Location).FileMinorPart & System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly.Location).FileBuildPart)
    Public Shared Version As String = System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly.Location).FileMajorPart & "." & System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly.Location).FileMinorPart & "." & System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly.Location).FileBuildPart
    Public Shared LegalCopyright As String = System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly.Location).LegalCopyright
    Public Shared CompanyName As String = System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly.Location).CompanyName
    Public Shared ProductName As String = System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly.Location).ProductName
    Public Shared ExeName As String = myFile.Name(System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly.Location).InternalName, False)
    Public Shared ProcessName As String = Diagnostics.Process.GetCurrentProcess.ProcessName
    Public Shared selType As Integer
    Public Shared winStore As Boolean = True
    Private bError As Boolean

    Private Sub App_DispatcherUnhandledException(ByVal sender As Object, ByVal e As DispatcherUnhandledExceptionEventArgs) Handles MyClass.DispatcherUnhandledException
        If bError Then Exit Sub
        bError = True
        e.Handled = True

        Dim Form As New wpfError
        Form.myError = e
        Form.ShowDialog()

        End
    End Sub

    Public Class myGlobal
        Public Shared NR As String = Chr(13) & Chr(10)
        Public Shared mySystem As New clsSystem
    End Class

    Public Shared ReadOnly Property Icon As ImageSource
        Get
            Return myBitmap.UriToImageSource(New Uri("/" + ExeName + ";component/" + ExeName + ".ico", UriKind.Relative))
        End Get
    End Property

    Public Shared Function Title() As String
        Return ProductName + " " + Version
    End Function

End Class
