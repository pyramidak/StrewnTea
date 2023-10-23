Public Class wpfDialog

#Region " Properties "
    Public Enum Ikona
        ok
        lupa
        chyba
        varovani
        heslo
        dotaz
        tuzka
    End Enum

    Public WriteOnly Property Delka() As Integer
        Set(ByVal value As Integer)
            txtHeslo.MaxLength = value
        End Set
    End Property

    Public WriteOnly Property Hlavicka() As String
        Set(ByVal value As String)
            Me.Title = value
        End Set
    End Property

    Public WriteOnly Property Vzkaz() As String
        Set(ByVal value As String)
            lblVzkaz.Text = value
        End Set
    End Property

    Public Property Input() As String
        Get
            Return txtHeslo.Text
        End Get
        Set(ByVal value As String)
            txtHeslo.Text = value
        End Set
    End Property

    Private bRemember As Boolean
    Public Property Zatrzeno() As Boolean
        Get
            Return CBool(chkHeslo.IsChecked)
        End Get
        Set(ByVal value As Boolean)
            chkHeslo.IsChecked = value
        End Set
    End Property

    Private bSafeInput As Boolean

    Sub New(Owner As Window, Message As String, Header As String, Icon As Ikona, Optional ButtonOK As String = "OK", Optional ButtonCancel As String = "", Optional TextBoxVisible As Boolean = False, Optional CheckBoxVisible As Boolean = False, Optional CheckBoxText As String = "", Optional InputLength As Integer = 30, Optional InputSafe As Boolean = False)
        InitializeComponent()

        If Owner Is Nothing Then
            Me.ShowInTaskbar = True
        Else
            Me.Owner = Owner
        End If
        lblVzkaz.Text = Message
        Me.Title = Header
        imgIcon.Source = CType(Me.FindResource(Icon.ToString), ImageSource)
        OK_Button.Content = ButtonOK
        Cancel_Button.Content = ButtonCancel
        Cancel_Button.Visibility = If(ButtonCancel = "", Visibility.Hidden, Visibility.Visible)
        txtHeslo.Visibility = If(TextBoxVisible, Visibility.Visible, Visibility.Collapsed)
        chkHeslo.Visibility = If(CheckBoxVisible, Visibility.Visible, Visibility.Hidden)
        chkHeslo.Content = CheckBoxText
        txtHeslo.MaxLength = InputLength
        bSafeInput = InputSafe
        imgIcon.Opacity = If(Icon.ToString = "tuzka" Or Icon.ToString = "heslo", 0.5, 0.3)
    End Sub
#End Region

    Private Sub OK_Button_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles OK_Button.Click
        Me.DialogResult = True
        Me.Close()
    End Sub

    Private Sub Cancel_Button_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Cancel_Button.Click
        Me.DialogResult = False
        txtHeslo.Text = ""
        Me.Close()
    End Sub

    Private Sub txtHeslo_TextChanged(sender As System.Object, e As System.EventArgs) Handles txtHeslo.TextChanged
        If bSafeInput Then txtHeslo.Text = GetNameSafe(txtHeslo.Text)
    End Sub

    Private Sub wpfDialog_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        txtHeslo.Focus()
    End Sub

    Private Function GetNameSafe(ByVal Name As String) As String
        Name = Name.Replace("?", "")
        Name = Name.Replace("'", "")
        Name = Name.Replace("*", "")
        Name = Name.Replace("/", "")
        Name = Name.Replace("<", "")
        Name = Name.Replace(">", "")
        Name = Name.Replace(":", "")
        Name = Name.Replace("\", "")
        Name = Name.Replace(Chr(34), "")
        Return Name
    End Function

End Class
