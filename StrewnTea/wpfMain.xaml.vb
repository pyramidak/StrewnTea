Imports System.Windows.Threading
Imports System.Data
Imports System.ComponentModel
Imports System.Collections.ObjectModel
Imports System.Collections.Specialized

Class wpfMain

#Region " Declarations "

    Public dsDic As New Dictionary
    Public Const NR As String = Chr(13) & Chr(10)
    Public CestaDB As String
    Public DocumentsPath As String = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) & "\pyramidak\hanyu.xml"
    Private ZJShareMem As New clsSharedMemory
    Public WinName, FontName As String
    Public WinNum As Integer
    Private FindMode As Boolean
    Private txtPinyin As TextBox
    Private scrollViewer As ScrollViewer
    Private AppVerze As String
    Private myCloud As New clsCloud

#End Region

#Region " Loaded "

    Private Sub wpfMain_SizeChanged(sender As Object, e As System.Windows.SizeChangedEventArgs) Handles Me.SizeChanged
        panPinyin.Visibility = Windows.Visibility.Hidden
    End Sub

    Sub New()
        InitializeComponent()
        AddHandler DirectCast(ListView1.Items, System.Collections.Specialized.INotifyCollectionChanged).CollectionChanged, AddressOf ListView1_CollectionChanged
        EventManager.RegisterClassHandler(GetType(ScrollViewer), MouseWheelEvent, New MouseWheelEventHandler(AddressOf ListView_MouseWheel))
    End Sub

    Private Sub wpfMain_Closing(ByVal sender As Object, ByVal e As System.ComponentModel.CancelEventArgs) Handles Me.Closing
        proSaveDB()
    End Sub

    Private Sub wMain_Loaded(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles MyBase.Loaded
        Dim gv As GridView = CType(ListView1.View, GridView)
        For Each one As GridViewColumn In gv.Columns
            Dim ch As GridViewColumnHeader = TryCast(one.Header, GridViewColumnHeader)
            If ch Is Nothing Then
                For Each Control As Object In DirectCast(one.Header, Grid).Children
                    If TypeOf Control Is CheckBox Then
                        Dim ckb As CheckBox = CType(Control, CheckBox)
                        AddHandler ckb.Unchecked, AddressOf ckbNeumim_Unchecked
                    End If
                Next
            Else
                AddHandler ch.Click, AddressOf SortClick
            End If
        Next
        If Application.winStore Then smiUpdate.Visibility = Visibility.Collapsed

        lblApp.Text = Application.CompanyName & "  " & Application.ProductName & "  verze " & Application.Version
        lblCop.Text = "copyright ©2010-2015" & "  " & Application.LegalCopyright
        Me.Title += " " + Application.Version
        LoadLicense()

        CestaDB = myRegister.GetValue(HKEY.CURRENT_USER, "Software\pyramidak\StrewnTea", "Database", "")
        smiDropbox.IsEnabled = myCloud.DropBoxExist
        smiGoogleDrive.IsEnabled = myCloud.GoogleDriveExist
        smiOneDrive.IsEnabled = myCloud.OneDriveExist
        If CestaDB.StartsWith(myCloud.DropBoxFolder) And myCloud.DropBoxExist Then
            CestaDB = System.IO.Path.Combine(myCloud.DropBoxFolder, "pyramidak\hanyu.xml")
            smiDropbox.IsChecked = True
        ElseIf CestaDB.StartsWith(myCloud.GoogleDriveFolder) And myCloud.GoogleDriveExist Then
            CestaDB = System.IO.Path.Combine(myCloud.GoogleDriveFolder, "pyramidak\hanyu.xml")
            smiGoogleDrive.IsChecked = True
        ElseIf CestaDB.StartsWith(myCloud.OneDriveFolder) And myCloud.OneDriveExist Then
            CestaDB = System.IO.Path.Combine(myCloud.OneDriveFolder, "pyramidak\hanyu.xml")
            smiOneDrive.IsChecked = True
        ElseIf CestaDB.StartsWith(myCloud.SyncFolder) And myCloud.SyncExist Then
            CestaDB = System.IO.Path.Combine(myCloud.SyncFolder, "pyramidak\hanyu.xml")
            smiSync.IsChecked = True
        Else
            CestaDB = DocumentsPath
        End If

        'deployment vlastních lekcí slovíček
        If myFile.Exist(CestaDB) = False Then
            myFolder.Create(myFile.Path(CestaDB))
            Dim Stream As System.IO.Stream = System.Reflection.Assembly.GetExecutingAssembly.GetManifestResourceStream("RootSpace.hanyu.xml")
            Using inFile As New System.IO.BinaryReader(Stream)
                Using outFile As New System.IO.FileStream(CestaDB, IO.FileMode.Create, IO.FileAccess.Write)
                    Dim fileByte() As Byte = inFile.ReadBytes(CInt(Stream.Length))
                    outFile.Write(fileByte, 0, CInt(Stream.Length))
                End Using
            End Using
        End If

        LoadDataSetWithListView()
        cbxDirection.SelectedIndex = 0 : cbxType.SelectedIndex = 0
        cbxAnswers.SelectedIndex = 0 : cbxRestart.SelectedIndex = 0
    End Sub

#Region " Load License "

    Private Sub lbl_MouseLeftButtonUp(sender As System.Object, e As System.Windows.Input.MouseButtonEventArgs) Handles txtMail.MouseLeftButtonUp, txtWeb.MouseLeftButtonUp
        Dim lbl As TextBlock = CType(sender, TextBlock)
        Try
            Process.Start(lbl.Text)
        Catch ex As Exception
        End Try
    End Sub

    Private Sub lbl_MouseEnter(sender As System.Object, e As System.Windows.Input.MouseEventArgs) Handles txtMail.MouseEnter, txtWeb.MouseEnter
        Dim lbl As TextBlock = CType(sender, TextBlock)
        Me.Cursor = Cursors.Hand
    End Sub

    Private Sub lbl_MouseLeave(sender As System.Object, e As System.Windows.Input.MouseEventArgs) Handles txtMail.MouseLeave, txtWeb.MouseLeave
        Dim lbl As TextBlock = CType(sender, TextBlock)
        Me.Cursor = Cursors.Arrow
    End Sub

    Private Sub LoadLicense()
        Dim Stream As System.IO.Stream = System.Reflection.Assembly.GetExecutingAssembly.GetManifestResourceStream("RootSpace.License.txt")
        Dim Reader As New System.IO.BinaryReader(Stream)
        Dim fileByte() As Byte = Reader.ReadBytes(CInt(Stream.Length))
        Reader.Close() : Stream.Close()
        txtLicense.Text = ByteToString(fileByte)
    End Sub

    Private Function ByteToString(ByVal arrBytes() As Byte) As String
        Dim Text As String = ""
        Dim i As Integer
        For i = LBound(arrBytes) To UBound(arrBytes)
            Text = Text & Chr(arrBytes(i))
        Next
        Return Text
    End Function
#End Region

#End Region

#Region " Dataset "

    Private Sub LoadDataSetWithListView()
        dsDic = New Dictionary
        If myFile.Exist(CestaDB) Then
            dsDic.ReadXml(CestaDB)
        Else
            dsDic.Main.AddMainRow("prosím, nezdvořilý", "不客气", "bú kè qi", 0, False, 0)
        End If

        With dsDic.Main.Columns
            If .Contains("found") = False Then .Add("found", GetType(System.Boolean))
        End With

        ListView1.BeginInit()
        ListView1.ItemsSource = dsDic.Main.DefaultView
        ListView1.EndInit()
        lblLoading.Visibility = Windows.Visibility.Hidden

        UpdateLesson()
        UpdateResults()

        AddRow(True)

        dsDic.AcceptChanges()
    End Sub

    Private Sub miDel_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles miDel.Click
        RemoveRow()
    End Sub

    Private Sub RemoveRow(Optional ByVal EndRow As Boolean = False)
        If EndRow Then
            Dim dr As Dictionary.MainRow = dsDic.Main(dsDic.Main.Count - 1)
            If Not dr.RowState = DataRowState.Deleted Then
                If dr.jieke = "" Then dr.Delete()
            End If
        Else
            ListView1.SelectedItems.Remove(ListView1.Items(ListView1.Items.Count - 1))
            While ListView1.SelectedItems.Count > 0
                CType(ListView1.SelectedItem, DataRowView).Delete()
            End While
            btnRepeat.IsEnabled = False : btnNext.IsEnabled = False
            ckbPinyin.IsEnabled = False : txtAnswer.IsEnabled = False
        End If
    End Sub

    Private Sub miAdd_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles miAdd.Click
        AddRow()
    End Sub

    Public Sub AddRow(Optional ByVal EndRow As Boolean = False)
        Dim dr As Dictionary.MainRow = dsDic.Main.NewMainRow
        dr.hanzi = "" : dr.pinyin = "" : dr.jieke = "" : dr.learned = False : dr.tested = 0
        dr.lesson = If(ListView1.SelectedIndex = -1, dsDic.Main(dsDic.Main.Count - 1).lesson, dsDic.Main(ListView1.SelectedIndex).lesson)
        If ListView1.SelectedIndex = -1 Or EndRow = True Then
            If Not dsDic.Main(dsDic.Main.Count - 1).jieke = "" Then
                dsDic.Main.AddMainRow(dr)
            End If
        Else
            dsDic.Main.Rows.InsertAt(dr, ListView1.SelectedIndex)
        End If
    End Sub

#Region " Create GridViewColumns programatically "

    Private Function GetGridView() As GridView
        Return CType(ListView1.View, GridView)
    End Function

    'ListView1.View = CreateGridViewColumns(dsDic.Main)

    Private Function CreateGridViewColumns(ByVal dt As DataTable) As GridView
        ' Create the GridView
        Dim gv As New GridView()
        gv.AllowsColumnReorder = True
        ' Create the GridView Columns
        For Each item As DataColumn In dt.Columns
            Dim gvc As New GridViewColumn()
            gvc.DisplayMemberBinding = New Binding(item.ColumnName)
            gvc.Header = item.ColumnName
            gvc.Width = [Double].NaN
            gv.Columns.Add(gvc)
        Next
        Return gv
    End Function
#End Region

    Private Sub proSaveDB(Optional ByVal bInfo As Boolean = False)
        myRegister.CreateValue(HKEY.CURRENT_USER, "Software\pyramidak\StrewnTea", "Database", CestaDB)
        RemoveRow(True)
        If dsDic.HasChanges Then
            proCancelFind()
            Try
                With dsDic.Main.Columns
                    If .Contains("found") Then .Remove("found")
                    myFolder.Exist(myFolder.Path(CestaDB), True)
                    dsDic.WriteXml(CestaDB)
                    If .Contains("found") = False Then .Add("found", GetType(System.Boolean))
                    dsDic.AcceptChanges()
                End With
                If bInfo Then
                    Dim wDialog = New wpfDialog(Me, "Uloženo do:" + NR + CestaDB, Me.Title, wpfDialog.Ikona.ok, "Zavřít")
                    wDialog.ShowDialog()
                End If
            Catch ex As Exception
                Dim wDialog = New wpfDialog(Me, "Chyba: " + Err.Number.ToString + NR + ex.Message, Me.Title, wpfDialog.Ikona.chyba, "Zavřít")
                wDialog.ShowDialog()
            End Try
        End If
        UpdateLesson()
        UpdateResults()
        dsDic.AcceptChanges()
    End Sub

    Private Sub cmsDGV_Opened(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles cmsDGV.Opened
        miAdd.IsEnabled = Not FindMode
        miDel.IsEnabled = If(dsDic.Main.Rows.Count < 3 Or ListView1.SelectedItems.Count < 1 Or ListView1.SelectedIndex = ListView1.Items.Count - 1, False, True)
    End Sub

    Private Sub smiMenu_SubmenuOpened(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles smiMenu.SubmenuOpened
        smiSave.IsEnabled = dsDic.HasChanges
        smiPrint.IsEnabled = If(ListView1.Items.Count > 2, True, False)
    End Sub

    Private Sub smiSave_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles smiSave.Click
        proSaveDB(True)
        AddRow(True)
        dsDic.AcceptChanges()
    End Sub

    Private Sub smiImport_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles smiImport.Click
        Dim wDialog = New wpfDialog(Me, "V každém řádku musí textový soubor" + NR + _
                      "obsahovat tři hodnoty oddělené ;" + NR + _
                      "překlad ; chànzì ; pīnyīn", Me.Title, Nothing, "Zavřít")
        wDialog.ShowDialog()
        ' Configure open file dialog box 
        Dim dlg As New Microsoft.Win32.OpenFileDialog()
        dlg.Title = "Vyberte textový soubor obsahující slovíčka"
        dlg.Filter = "Textový soubor|*.txt|StrewnTea file|hanyu.xml"
        'dlg.FileName = "Document" ' Default file name 
        'dlg.DefaultExt = ".txt" ' Default file extension 

        If dlg.ShowDialog = True Then
            If dlg.FileName.ToLower.Contains(".xml") Then
                If myFile.Exist(CestaDB) Then
                    Dim CestaBak As String = CestaDB + "." + TimeOfDay.Second.ToString
                    If myFile.Copy(dlg.FileName, CestaDB) Then
                        LoadDataSetWithListView()
                    End If
                End If
            Else
                Try
                    RemoveRow(True)
                    Dim Reader As New IO.StreamReader(dlg.FileName, True)
                    Dim Lekce As Integer = 0
                    Do Until Reader.EndOfStream
                        Dim sRadek() As String = Split(Reader.ReadLine, Chr(34) & ";" & Chr(34))
                        If sRadek(0).StartsWith("lekce") Then
                            Lekce += 1
                        Else
                            dsDic.Main.AddMainRow(sRadek(0).Trim, sRadek(1).Trim, sRadek(2).Trim, Lekce, False, 0)
                        End If
                    Loop
                    ListView1.ItemsSource = dsDic.Main.DefaultView
                    AddRow(True)
                Catch ex As Exception
                    Dim wDialog2 = New wpfDialog(Me, "Chyba: " + Err.Number.ToString + NR + ex.Message, Me.Title, wpfDialog.Ikona.chyba, "Zavřít")
                    wDialog2.ShowDialog()
                End Try
            End If
        End If
    End Sub

#End Region

#Region " TabControl "

    Private Sub TabControl1_SelectionChanged(sender As System.Object, e As System.Windows.Controls.SelectionChangedEventArgs) Handles TabControl1.SelectionChanged
        Select Case TabControl1.SelectedIndex
            Case 0
                ckbPinyin.IsChecked = False

            Case 1
                panPinyin.Visibility = Windows.Visibility.Hidden
                btnStart.IsEnabled = If(dsDic.Main.Rows.Count < 2, False, True)

        End Select
    End Sub
#End Region

#Region " ListView "

    Private Sub ListView1_SelectionChanged(sender As System.Object, e As System.Windows.Controls.SelectionChangedEventArgs) Handles ListView1.SelectionChanged
        panPinyin.Visibility = Windows.Visibility.Hidden
        AddRow(True)

        'GET COORDINATES OF SELECTED ITEM
        'Dim selectedContainer As UIElement = DirectCast(ListView1.ItemContainerGenerator.ContainerFromIndex(ListView1.SelectedIndex), UIElement)
        'Dim cursorPos As Point = selectedContainer.TranslatePoint(New Point(selectedContainer.DesiredSize.Width, 0), Me)
    End Sub

    Private Sub ListView1_CollectionChanged(sender As Object, e As NotifyCollectionChangedEventArgs)
        If e.Action = NotifyCollectionChangedAction.Add Then
            ListView1.ScrollIntoView(e.NewItems(0))
        End If
    End Sub

    Private Sub ckbNeumim_Unchecked(sender As System.Object, e As System.Windows.RoutedEventArgs)
        Dim ckb As CheckBox = CType(sender, CheckBox)
        UncheckAllLearned()
        ckb.IsChecked = True
    End Sub

    Private Sub UncheckAllLearned()
        If ListView1.Items.Count = 0 Then Exit Sub
        If ListView1.SelectedItems.Count = 0 Then Exit Sub
        Dim wDialog = New wpfDialog(Me, "Chcete nastavit vybraná slovíčka, že je neumíte?", Me.Title, wpfDialog.Ikona.dotaz, "Ano", "Ne")
        If wDialog.ShowDialog Then
            For Each one As DataRowView In ListView1.SelectedItems
                Dim dr As Dictionary.MainRow = CType(one.Row, Dictionary.MainRow)
                dr.learned = False
            Next
        End If
    End Sub

    Private Sub miUncheck_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles miUncheck.Click
        UncheckAllLearned()
    End Sub

    Private Sub ListView_MouseWheel(sender As System.Object, e As System.Windows.Input.MouseWheelEventArgs) Handles ScrollViewer2.MouseWheel
        panPinyin.Visibility = Windows.Visibility.Hidden
        If e.Delta > 0 Then
            ScrollViewer2.PageLeft()
        Else
            ScrollViewer2.PageRight()
        End If
    End Sub

#Region " Change Styles "

    Public Shared Sub WaitForPriority(priority As DispatcherPriority)
        Dim frame As New DispatcherFrame()
        Dim dispatcherOperation As DispatcherOperation = Dispatcher.CurrentDispatcher.BeginInvoke(priority, New DispatcherOperationCallback(AddressOf ExitFrameOperation), frame)
        Dispatcher.PushFrame(frame)
        If dispatcherOperation.Status <> DispatcherOperationStatus.Completed Then
            dispatcherOperation.Abort()
        End If
    End Sub

    Private Shared Function ExitFrameOperation(obj As Object) As Object
        DirectCast(obj, DispatcherFrame).Continue = False
        Return Nothing
    End Function

    Private Sub ShowLoading()
        lblLoading.Visibility = Windows.Visibility.Visible
    End Sub

    Private Sub btnGrid_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles btnGrid.Click
        lblLoading.Visibility = Windows.Visibility.Visible
        WaitForPriority(DispatcherPriority.Background)
        ListView1.Style = DirectCast(Me.FindResource("GridStyle"), Style)
        lblLoading.Visibility = Windows.Visibility.Hidden
    End Sub

    Private Sub btnList_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles btnList.Click
        lblLoading.Visibility = Windows.Visibility.Visible
        WaitForPriority(DispatcherPriority.Background)
        ListView1.Style = DirectCast(Me.FindResource("ListStyle"), Style)
        lblLoading.Visibility = Windows.Visibility.Hidden
    End Sub
#End Region

#Region " ListView Sort "

    Private _CurSortCol As GridViewColumnHeader = Nothing
    Private _CurAdorner As SortAdorner = Nothing

    Private Sub SortClick(ByVal sender As Object, ByVal e As RoutedEventArgs)
        Dim field As String
        Dim column As GridViewColumnHeader
        Dim newDir As ListSortDirection
        Try
            column = TryCast(sender, GridViewColumnHeader)
            field = column.Tag.ToString

            If _CurSortCol IsNot Nothing Then
                AdornerLayer.GetAdornerLayer(_CurSortCol).Remove(_CurAdorner)
                ListView1.Items.SortDescriptions.Clear()
            End If

            newDir = ListSortDirection.Ascending
            If IsNothing(_CurSortCol) = False Then
                If _CurSortCol.Name = column.Name AndAlso _CurAdorner.Direction = newDir Then
                    newDir = ListSortDirection.Descending
                End If
            End If

            _CurSortCol = column
            _CurAdorner = New SortAdorner(_CurSortCol, newDir)
            AdornerLayer.GetAdornerLayer(_CurSortCol).Add(_CurAdorner)

            ListView1.Items.SortDescriptions.Add(New SortDescription(field, newDir))
        Catch ex As Exception
        End Try
    End Sub
#End Region

#Region " Find Control in ListViewItem "

    'FIND CONTROL IN ITEM
    'If ListView1.SelectedItem IsNot Nothing Then
    '   Dim o As Object = ListView1.SelectedItem
    '  Dim lvi As ListViewItem = DirectCast(ListView1.ItemContainerGenerator.ContainerFromItem(o), ListViewItem)
    ' Dim tb As TextBox = TryCast(FindByName("txtPinyin", lvi), TextBox)
    '
    '   If tb IsNot Nothing Then
    'tb.Dispatcher.BeginInvoke(New Func(Of Boolean)(AddressOf tb.Focus))
    '   End If
    'End If

    Private Function FindByName(name As String, root As FrameworkElement) As FrameworkElement
        Dim tree As New Stack(Of FrameworkElement)()
        tree.Push(root)

        While tree.Count > 0
            Dim current As FrameworkElement = tree.Pop()
            If current.Name = name Then
                Return current
            End If

            Dim count As Integer = VisualTreeHelper.GetChildrenCount(current)
            For i As Integer = 0 To count - 1
                Dim child As DependencyObject = VisualTreeHelper.GetChild(current, i)
                If TypeOf child Is FrameworkElement Then
                    tree.Push(DirectCast(child, FrameworkElement))
                End If
            Next
        End While

        Return Nothing
    End Function
#End Region

#End Region

#Region " Pinyin Keyboard "

    Private Sub txtPinyin_GotFocus(sender As System.Object, e As System.Windows.RoutedEventArgs)
        txtPinyin = TryCast(sender, TextBox)
        If txtPinyin IsNot Nothing Then
            Dim relativePoint As Point = txtPinyin.TransformToAncestor(Me).Transform(New Point(0, 0))
            Dim iHeight As Double = 0
            If relativePoint.Y + panPinyin.Height + txtPinyin.ActualHeight > Me.ActualHeight Then
                iHeight = relativePoint.Y - panPinyin.ActualHeight
            Else
                iHeight = relativePoint.Y + txtPinyin.ActualHeight
            End If
            panPinyin.Margin = New System.Windows.Thickness(relativePoint.X, iHeight, 0, 0)
            panPinyin.Visibility = Windows.Visibility.Visible
        End If
    End Sub

    Private Sub btnPinyin_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles btnA1.Click, btnA2.Click, btnA3.Click, btnA4.Click, btnE1.Click, btnE2.Click, btnE3.Click, btnE4.Click, btnI1.Click, btnI2.Click, btnI3.Click, btnI4.Click, btnU1.Click, btnU2.Click, btnU3.Click, btnU4.Click, btnUU1.Click, btnUU2.Click, btnUU3.Click, btnUU4.Click, btnO1.Click, btnO2.Click, btnO3.Click, btnO4.Click
        Dim btn As Button = CType(sender, Button)
        If TabControl1.SelectedIndex = 0 Then
            txtPinyin.SelectedText = btn.Content.ToString
            txtPinyin.CaretIndex += txtPinyin.SelectedText.Length
            txtPinyin.SelectionLength = 0
            txtPinyin.Focus()
        Else
            txtAnswer.SelectedText = btn.Content.ToString
            txtAnswer.CaretIndex += txtAnswer.SelectedText.Length
            txtAnswer.SelectionLength = 0
            txtAnswer.Focus()
        End If
    End Sub

    Private Sub ckbPinyin_Checked(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles ckbPinyin.Checked
        Dim relativePoint As Point = txtAnswer.TransformToAncestor(Me).Transform(New Point(0, 0))
        panPinyin.Margin = New System.Windows.Thickness(relativePoint.X, relativePoint.Y + txtAnswer.ActualHeight, 0, 0)
        panPinyin.Visibility = Windows.Visibility.Visible
        panPinyin.Focus()
    End Sub

    Private Sub ckbPinyin_UnChecked(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles ckbPinyin.Unchecked
        panPinyin.Visibility = Windows.Visibility.Hidden
        txtAnswer.Focus()
    End Sub

#End Region

#Region " Test "
    Private iDirection, iLesson, iRepetition, iAnswer, iFirst, iLast, iDrawnDirection As Integer
    Private bRestart, bDale As Boolean
    Private drTest As Dictionary.MainRow
    Private sCech(), sChan(), sZnaky() As String

#Region " Setting "

    Private Sub cbxLesson_SelectionChanged(sender As System.Object, e As System.Windows.Controls.SelectionChangedEventArgs) Handles cbxLesson.SelectionChanged
        lblWrong.Text = "0"
    End Sub

    Private Sub UpdateLesson()
        Dim LastLesson As String = cbxLesson.Text
        cbxLesson.Items.Clear()
        cbxLesson.Items.Add("All")
        For Each Row As Dictionary.MainRow In dsDic.Main.Rows
            If Not Row.RowState = DataRowState.Deleted Then
                If FindStrInCbx(cbxLesson, Row.lesson.ToString) = False Then cbxLesson.Items.Add(Row.lesson)
            End If
        Next
        cbxLesson.Text = If(FindStrInCbx(cbxLesson, LastLesson) = False, "All", LastLesson)
    End Sub

    Private Function FindStrInCbx(ByVal cbx As ComboBox, ByVal str As String) As Boolean
        For Each one As Object In cbx.Items
            If one.ToString = str Then Return True
        Next
        Return False
    End Function

    Private Sub UpdateResults()
        Dim iKnowledge, iLearn As Integer
        Dim sKnowledge As String = ""
        Dim sLearn As String = ""
        Dim sKnowledge2 As String = ""
        Dim sLearn2 As String = ""

        iKnowledge = 0
        For Each Row As Dictionary.MainRow In dsDic.Main.Rows
            If Not Row.RowState = DataRowState.Deleted Then
                If Row.learned Then
                    iKnowledge += 1
                    sKnowledge += Row.hanzi
                Else
                    sLearn += Row.hanzi
                End If
            End If
        Next

        For a As Integer = 0 To sKnowledge.Length - 1
            If sKnowledge2.Contains(sKnowledge.Substring(a, 1)) = False Then
                sKnowledge2 += sKnowledge.Substring(a, 1)
            End If
        Next
        For a As Integer = 0 To sLearn.Length - 1
            If Not sLearn2.Contains(sLearn.Substring(a, 1)) And Not sKnowledge2.Contains(sLearn.Substring(a, 1)) Then
                sLearn2 += sLearn.Substring(a, 1)
            End If
        Next

        iLearn = dsDic.Main.Rows.Count - iKnowledge
        pgbTotal.Minimum = 0 : pgbTotal.Maximum = dsDic.Main.Rows.Count : pgbTotal.Value = iKnowledge
        lblKnowledge.Text = iKnowledge.ToString : lblLearn.Text = iLearn.ToString
        lblKhanzi.Text = sKnowledge2.Length.ToString : lblUhanzi.Text = sLearn2.Length.ToString
    End Sub
#End Region

#Region " Start "

    Private Sub btnStart_Click_1(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles btnStart.Click
        proSaveDB()

        'Nastavení do integru
        iDirection = cbxDirection.SelectedIndex
        iLesson = If(cbxLesson.SelectedIndex = 0, -1, CInt(cbxLesson.Text))
        iRepetition = cbxType.SelectedIndex + 1
        iAnswer = cbxAnswers.SelectedIndex + 1
        bRestart = CBool(cbxRestart.SelectedIndex)
        bDale = True
        iFirst = -1 : iLast = -1

        'Nastavení rozsahu náhodného čísla
        If iLesson = -1 Then
            iFirst = 0 : iLast = dsDic.Main.Rows.Count - 1
        Else
            For a As Integer = 0 To dsDic.Main.Rows.Count - 1
                If dsDic.Main(a).lesson = iLesson And iFirst = -1 Then iFirst = a
                If dsDic.Main(a).lesson <> iLesson And iFirst <> -1 Then iLast = a - 1 : Exit For
            Next
            If iLast = -1 Then iLast = dsDic.Main.Rows.Count - 1
        End If

        'Kontrola zda poslední test byl dokončen
        If bRestart = False Then
            bDale = False
            For a As Integer = iFirst To iLast
                If dsDic.Main(a).tested = 0 Then bDale = True : Exit For
            Next
        End If

        'Nastavení tested
        If bRestart Then
            For a As Integer = iFirst To iLast
                dsDic.Main(a).tested = 0
                If iRepetition = 3 Then
                    If dsDic.Main(a).learned = True Then
                        dsDic.Main(a).tested = 1
                    End If
                End If
            Next
        End If

        'Results
        pgbTest.Minimum = 0 : pgbTest.Maximum = iLast - iFirst + 1 : pgbTest.Value = 1
        lblTotal.Text = pgbTest.Maximum.ToString

        NextRandom()
    End Sub

    Private Sub NextRandom()
        btnNext.Content = "Prozradit"
        btnRepeat.IsEnabled = False

        'Kontrola, zda už není vše zodpovězeno
        Dim iAnswered, iCorrect As Integer
        For a As Integer = iFirst To iLast
            If dsDic.Main(a).tested > 0 Then iAnswered += 1
            If dsDic.Main(a).tested = 1 And dsDic.Main(a).learned Then iCorrect += 1
        Next
        pgbTest.Value = iAnswered
        lblCorrect.Text = iCorrect.ToString
        If iAnswered = iLast - iFirst + 1 Then
            btnNext.IsEnabled = False
            txtAnswer.IsEnabled = False
            ckbPinyin.IsEnabled = False : ckbPinyin.IsChecked = False
            If bDale Then
                Dim wDialog = New wpfDialog(Me, "Test dokončen.", Me.Title, wpfDialog.Ikona.ok, "Zavřít")
                wDialog.ShowDialog()
            End If
            UpdateResults()
            Exit Sub
        End If

        'Losování další otázky
        Dim R As New Random(Now.Millisecond)
        Do
            drTest = dsDic.Main(R.Next(iFirst, iLast + 1))
        Loop Until drTest.tested = 0

        'Příprava českých odpovědí
        sCech = Split(delZavorky(drTest.jieke.ToLower), ",")
        For a As Integer = 0 To sCech.GetUpperBound(0)
            sCech(a) = Trim(sCech(a))
        Next

        'Příprava čínských odpovědí
        sChan = Split(delZavorky(drTest.pinyin.ToLower), " ")
        If Not sChan.GetUpperBound(0) = 3 Then ReDim Preserve sChan(3)

        'Příprava čínských znaků
        ReDim sZnaky(3)
        For a As Integer = 0 To drTest.hanzi.Length - 1
            sZnaky(a) = drTest.hanzi.Substring(0 + a, 1)
        Next

        'Zobrazení otázek
        proCleanQuestions()
        iDrawnDirection = If(iDirection = 2, R.Next(0, 2), iDirection)
        If iDrawnDirection = 0 Then
            lblQuestion.Text = drTest.jieke
        Else
            lblZ1.Text = sZnaky(0) : lblZ2.Text = sZnaky(1) : lblZ3.Text = sZnaky(2) : lblZ4.Text = sZnaky(3)
        End If

        txtAnswer.IsEnabled = True : btnNext.IsEnabled = True : ckbPinyin.IsEnabled = True
        txtAnswer.Focus()
    End Sub

    Private Function delZavorky(ByVal sText As String) As String
        If Not sText.IndexOf("(") = -1 Then
            sText = sText.Substring(0, sText.IndexOf("("))
        End If
        Return Trim(sText)
    End Function

    Private Sub proCleanQuestions()
        txtAnswer.Text = ""
        lblQuestion.Text = ""
        lblL1.Text = "" : lblL2.Text = "" : lblL3.Text = "" : lblL4.Text = ""
        lblZ1.Text = "" : lblZ2.Text = "" : lblZ3.Text = "" : lblZ4.Text = ""
    End Sub

#End Region

#Region " Answers "

    Private Sub btnNext_Click_1(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles btnNext.Click
        If btnNext.Content.ToString = "Prozradit" Then
            proReveal()
        Else
            NextRandom()
        End If
    End Sub

    Private Sub btnRepeat_Click_1(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles btnRepeat.Click
        drTest.learned = False
        If iRepetition = 2 Then drTest.tested = 0
        NextRandom()
    End Sub


    Private Sub txtAnswer_TextChanged_1(sender As System.Object, e As System.Windows.Controls.TextChangedEventArgs) Handles txtAnswer.TextChanged
        Dim sAnswer As String = Trim(txtAnswer.Text).ToLower
        If iDrawnDirection = 0 Then
            If iAnswer = 1 Then
                Dim AnswerParts() As String
                If sAnswer.Contains(" ") Then
                    AnswerParts = Split(sAnswer, " ")
                Else
                    ReDim AnswerParts(sAnswer.Length)
                    For a As Integer = 0 To sAnswer.Length - 1
                        AnswerParts(a) = sAnswer.Substring(0 + a, 1)
                    Next
                    AnswerParts(sAnswer.Length) = sAnswer
                End If
                For Each Part As String In AnswerParts
                    If Part = sChan(0) Or Part = sZnaky(0) Then lblL1.Text = sChan(0) : lblZ1.Text = sZnaky(0)
                    If Part = sChan(1) Or Part = sZnaky(1) Then lblL2.Text = sChan(1) : lblZ2.Text = sZnaky(1)
                    If Part = sChan(2) Or Part = sZnaky(2) Then lblL3.Text = sChan(2) : lblZ3.Text = sZnaky(2)
                    If Part = sChan(3) Or Part = sZnaky(3) Then lblL4.Text = sChan(3) : lblZ4.Text = sZnaky(3)
                Next
                If lblZ1.Text + lblZ2.Text + lblZ3.Text + lblZ4.Text = drTest.hanzi Then proReveal(True)
                If Trim(lblL1.Text + " " + lblL2.Text + " " + lblL3.Text + " " + lblL4.Text) = drTest.pinyin Then proReveal(True)
            Else
                If txtAnswer.Text = drTest.hanzi Or txtAnswer.Text = drTest.pinyin Then proReveal(True)
            End If
        Else
            For Each Odpoved As String In sCech
                If txtAnswer.Text.ToLower = Odpoved.ToLower Then proReveal(True)
            Next
        End If
    End Sub

    Private Sub proReveal(Optional ByVal bCorrect As Boolean = False)
        drTest.tested = 1
        If bCorrect Then
            drTest.learned = True
            btnRepeat.IsEnabled = True
        Else
            lblWrong.Text = (CInt(lblWrong.Text) + 1).ToString
            drTest.learned = False
            If iRepetition = 2 Then drTest.tested = 0
        End If
        lblL1.Text = sChan(0) : lblL2.Text = sChan(1) : lblL3.Text = sChan(2) : lblL4.Text = sChan(3)
        lblZ1.Text = sZnaky(0) : lblZ2.Text = sZnaky(1) : lblZ3.Text = sZnaky(2) : lblZ4.Text = sZnaky(3)
        lblQuestion.Text = drTest.jieke
        txtAnswer.IsEnabled = False
        ckbPinyin.IsEnabled = False : ckbPinyin.IsChecked = False
        btnNext.Content = "Další"
    End Sub
#End Region

#Region " Copy character to clipboard "

    Private Sub lblZ1_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles lblZ1.MouseDown, lblZ2.MouseDown, lblZ3.MouseDown, lblZ4.MouseDown, lblL1.MouseDown, lblL2.MouseDown, lblL3.MouseDown, lblL4.MouseDown, lblQuestion.MouseDown
        Dim lblZ As TextBlock = CType(sender, TextBlock)
        If Not lblZ.Text = "" Then
            Try
                Clipboard.SetText(lblZ.Text)
            Catch ex As Exception
            End Try
        End If
    End Sub
#End Region

#End Region

#Region " Cloud Change "

    Private Sub smiDropBox_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles smiDropbox.Click
        smiDropbox.IsChecked = Not smiDropbox.IsChecked
        If smiDropbox.IsChecked Then
            smiGoogleDrive.IsChecked = False
            smiOneDrive.IsChecked = False
            smiSync.IsChecked = False
            ChangeLocation(System.IO.Path.Combine(myCloud.DropBoxFolder, "pyramidak\hanyu.xml"), CestaDB)
        Else
            ChangeLocation(DocumentsPath, CestaDB)
        End If
        LoadDataSetWithListView()
    End Sub

    Private Sub smiGoogleDrive_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles smiGoogleDrive.Click
        smiGoogleDrive.IsChecked = Not smiGoogleDrive.IsChecked
        If smiGoogleDrive.IsChecked Then
            smiDropbox.IsChecked = False
            smiOneDrive.IsChecked = False
            smiSync.IsChecked = False
            ChangeLocation(System.IO.Path.Combine(myCloud.GoogleDriveFolder, "pyramidak\hanyu.xml"), CestaDB)
        Else
            ChangeLocation(DocumentsPath, CestaDB)
        End If
        LoadDataSetWithListView()
    End Sub

    Private Sub smiOneDrive_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles smiOneDrive.Click
        smiOneDrive.IsChecked = Not smiOneDrive.IsChecked
        If smiOneDrive.IsChecked Then
            smiDropbox.IsChecked = False
            smiGoogleDrive.IsChecked = False
            smiSync.IsChecked = False
            ChangeLocation(System.IO.Path.Combine(myCloud.OneDriveFolder, "pyramidak\hanyu.xml"), CestaDB)
        Else
            ChangeLocation(DocumentsPath, CestaDB)
        End If
        LoadDataSetWithListView()
    End Sub

    Private Sub smiSync_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles smiSync.Click
        smiSync.IsChecked = Not smiSync.IsChecked
        If smiSync.IsChecked Then
            smiDropbox.IsChecked = False
            smiGoogleDrive.IsChecked = False
            smiOneDrive.IsChecked = False
            ChangeLocation(System.IO.Path.Combine(myCloud.SyncFolder, "pyramidak\hanyu.xml"), CestaDB)
        Else
            ChangeLocation(DocumentsPath, CestaDB)
        End If
        LoadDataSetWithListView()
    End Sub

    Private Sub ChangeLocation(NewCestaDB As String, OldCestaDB As String)
        CestaDB = NewCestaDB
        If myFile.Exist(NewCestaDB) Then
            Dim wDialog = New wpfDialog(Me, "V novém umístění již databáze existuje." + NR + NR + "Chcete použít nalezenou databázi" + NR + NewCestaDB _
                               + NR + NR + "nebo ji nahradit svoji databází?" + NR + OldCestaDB, Me.Title, wpfDialog.Ikona.dotaz, "Použít", "Nahradit")
            If wDialog.ShowDialog Then
                LoadDataSetWithListView()
                Exit Sub
            End If
        End If
        myFile.Copy(OldCestaDB, NewCestaDB)
    End Sub

#End Region

#Region " Find "

    Private Sub txtFind_KeyUp(sender As Object, e As System.Windows.Input.KeyEventArgs) Handles txtFind.KeyUp
        If e.Key = Key.Enter Then
            If txtFind.Text = "" Then
                proCancelFind()
                Exit Sub
            End If

            For Each dr As Dictionary.MainRow In dsDic.Main
                dr("found") = False
                If IsNumeric(txtFind.Text) Then
                    If dr.IslessonNull = False Then
                        If dr.lesson = CInt(txtFind.Text) Then dr("found") = True
                    End If
                End If
                If dr.IsjiekeNull = False Then
                    If dr.jieke.ToLower.Contains(txtFind.Text.ToLower) Then dr("found") = True
                End If
                If dr.IshanziNull = False Then
                    If dr.hanzi.ToLower.Contains(txtFind.Text.ToLower) Then dr("found") = True
                End If
                If dr.IspinyinNull = False Then
                    Dim sFind As String = dr.pinyin.ToLower
                    If sFind.Contains(txtFind.Text.ToLower) Then
                        dr("found") = True
                    Else
                        sFind = sFind.Replace("ū", "u") : sFind = sFind.Replace("ú", "u") : sFind = sFind.Replace("ŭ", "u") : sFind = sFind.Replace("ù", "u")
                        sFind = sFind.Replace("ǖ", "u") : sFind = sFind.Replace("ǘ", "u") : sFind = sFind.Replace("ǚ", "u") : sFind = sFind.Replace("ǜ", "u") : sFind = sFind.Replace("ü", "u")
                        sFind = sFind.Replace("ā", "a") : sFind = sFind.Replace("á", "a") : sFind = sFind.Replace("ă", "a") : sFind = sFind.Replace("à", "a")
                        sFind = sFind.Replace("ē", "e") : sFind = sFind.Replace("é", "e") : sFind = sFind.Replace("ĕ", "e") : sFind = sFind.Replace("è", "e")
                        sFind = sFind.Replace("ī", "i") : sFind = sFind.Replace("í", "i") : sFind = sFind.Replace("ĭ", "i") : sFind = sFind.Replace("ì", "i")
                        sFind = sFind.Replace("ō", "o") : sFind = sFind.Replace("ó", "o") : sFind = sFind.Replace("ŏ", "o") : sFind = sFind.Replace("ò", "o")
                        If sFind.Contains(txtFind.Text.ToLower) Then
                            dr("found") = True
                        End If
                    End If
                End If
            Next
            dsDic.Main.DefaultView.RowFilter = "found = 'true'"
            btnEndFind.IsEnabled = True
        End If
    End Sub

    Private Sub btnEndFind_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles btnEndFind.Click
        proCancelFind()
    End Sub

    Private Sub proCancelFind()
        dsDic.Main.DefaultView.RowFilter = ""
        btnEndFind.IsEnabled = False
        If Not ListView1.SelectedItems.Count = 0 Then
            ListView1.ScrollIntoView(ListView1.SelectedItem)
        End If
    End Sub
#End Region

#Region " Print "

    Private Sub smiPrint_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles smiPrint.Click
        Dim pd As New PrintDialog()
        If pd.ShowDialog() = True Then
            Dim Paginator As IDocumentPaginatorSource = CreateFlowDocument()
            pd.PrintDocument(Paginator.DocumentPaginator, "StrewnTea - slovíčka")
        End If
    End Sub

    Private Function CreateFlowDocument() As FlowDocument
        Dim doc = New FlowDocument
        Dim defFont As New FontFamily("Segoe UI")

        Dim tabMain As New Table()
        doc.Blocks.Add(tabMain)
        doc.ColumnGap = 10
        doc.FontFamily = defFont
        tabMain.Columns.Add(NewColumn(130))
        tabMain.Columns.Add(NewColumn(110))
        tabMain.Columns.Add(NewColumn(120))

        Dim grpHead As New TableRowGroup
        tabMain.RowGroups.Add(grpHead)
        Dim row As New TableRow
        grpHead.Rows.Add(row)
        row.Cells.Add(NewCell("Česky"))
        row.Cells.Add(NewCell("Čínsky"))
        row.Cells.Add(NewCell("Výslovnost"))

        'Čára
        row = New TableRow
        grpHead.Rows.Add(row)
        Dim cel = New TableCell
        row.Cells.Add(cel)
        cel.BorderThickness = New Thickness(0, 3, 0, 0)
        cel.BorderBrush = Brushes.Black
        cel.ColumnSpan = 3

        Dim grpSlova As New TableRowGroup
        tabMain.RowGroups.Add(grpSlova)
        Dim iLekce As Integer = -1
        For Each dr As Dictionary.MainRow In dsDic.Main
            If Not dr.RowState = DataRowState.Deleted Then
                If Not dr.jieke = "" Then
                    If Not dr.lesson = iLekce Then
                        iLekce = dr.lesson
                        row = New TableRow()
                        grpSlova.Rows.Add(row)
                        cel = NewCell("Lekce " + dr.lesson.ToString, 1, New Thickness(0, 0, 0, 5))
                        row.Cells.Add(cel)
                        cel.FontWeight = FontWeights.Bold
                        cel.FontSize = 14

                        If Not iLekce = 0 Then
                            cel.BorderThickness = New Thickness(0, 1, 0, 0)
                            cel.BorderBrush = Brushes.Black
                            cel.ColumnSpan = 3
                        End If
                    End If
                End If
            End If
            row = New TableRow()
            grpSlova.Rows.Add(row)
            row.Cells.Add(NewCell(dr.jieke))
            row.Cells.Add(NewCell(dr.hanzi))
            row.Cells.Add(NewCell(dr.pinyin))
            row.Cells(0).FontFamily = defFont
            row.Cells(1).FontFamily = New FontFamily("KaiTi")
            row.Cells(2).FontFamily = New FontFamily("Microsoft YaHei")
            row.Cells(0).FontSize = 12
            row.Cells(1).FontSize = 25
            row.Cells(2).FontSize = 14
        Next

        Dim grpAbout As New TableRowGroup
        tabMain.RowGroups.Add(grpAbout)
        row = New TableRow
        grpAbout.Rows.Add(row)
        cel = NewCell(lblApp.Text)
        row.Cells.Add(cel)
        cel.FontFamily = defFont
        cel.FontSize = 12
        cel.ColumnSpan = 3
        cel.BorderThickness = New Thickness(0, 1, 0, 0)
        cel.BorderBrush = Brushes.Black
        cel.ColumnSpan = 3
        row = New TableRow
        grpAbout.Rows.Add(row)
        cel = NewCell(lblCop.Text)
        row.Cells.Add(cel)
        cel.FontFamily = defFont
        cel.FontSize = 12
        cel.ColumnSpan = 3

        Return doc
    End Function

    Private Function NewColumn(ByVal iWidth As Integer) As TableColumn
        Dim col As New TableColumn
        col.Width = New GridLength(iWidth)
        Return col
    End Function

    Private Function NewParagraph(ByVal Obsah As String, Margin As Thickness) As Paragraph
        Dim prg As New Paragraph
        prg.Margin = Margin
        prg.Inlines.Add(New Run(Obsah))
        Return prg
    End Function

    Private Function NewCell(ByVal Obsah As String, Optional ByVal Span As Integer = 1, Optional Margin As Thickness = Nothing) As TableCell
        Dim tc As New TableCell
        tc.ColumnSpan = Span
        tc.Blocks.Add(NewParagraph(Obsah, Margin))
        Return tc
    End Function

#End Region

End Class

#Region " Dictionary "

Public Class clsDictionary

    'Private DocumentsPath As String = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) & "\ZJSoftware\hanyu.xml"
    Private dataset As Dictionary

    Public Sub New()
        dataset = New Dictionary
        dataset.Main.AddMainRow("prosím, nezdvořilý (note)", "不客气", "bú kè qi (note)", 0, False, 0)
        'dataset.ReadXml(DocumentsPath)
    End Sub

    Public Function GetDictionary() As DataView
        Return dataset.Main.DefaultView
    End Function
End Class

#End Region

#Region " Class Sort "

Public Class SortAdorner
    Inherits Adorner
    Private Shared ReadOnly _AscGeometry As Geometry = Geometry.Parse("M 0,5 L 10,5 L 5,0 Z")
    Private Shared ReadOnly _DescGeometry As Geometry = Geometry.Parse("M 0,0 L 10,0 L 5,5 Z")

    Public Property Direction() As ListSortDirection
        Get
            Return m_Direction
        End Get
        Private Set(ByVal value As ListSortDirection)
            m_Direction = value
        End Set
    End Property
    Private m_Direction As ListSortDirection

    Public Sub New(ByVal element As UIElement, ByVal dir As ListSortDirection)
        MyBase.New(element)
        Direction = dir
    End Sub

    Protected Overrides Sub OnRender(ByVal drawingContext As DrawingContext)
        MyBase.OnRender(drawingContext)

        If AdornedElement.RenderSize.Width < 20 Then
            Return
        End If

        drawingContext.PushTransform(New TranslateTransform(AdornedElement.RenderSize.Width - 15, (AdornedElement.RenderSize.Height - 5) / 2))

        drawingContext.DrawGeometry(Brushes.Black, Nothing, If(Direction = ListSortDirection.Ascending, _AscGeometry, _DescGeometry))

        drawingContext.Pop()
    End Sub
End Class

#End Region

#Region " Convertors "

Public Class BoolToVisibilityConverter
    Implements IValueConverter

    Public Function Convert(ByVal value As Object, ByVal targetType As Type, ByVal parameter As Object, ByVal culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.Convert
        Dim param As Boolean = Boolean.Parse(TryCast(parameter, String))
        Dim val As Boolean = CBool(value)

        Return If(val = param, Visibility.Visible, Visibility.Hidden)
    End Function

    Public Function ConvertBack(ByVal value As Object, ByVal targetType As System.Type, ByVal parameter As Object, ByVal culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.ConvertBack
        Throw New NotImplementedException()
    End Function
End Class

#End Region

