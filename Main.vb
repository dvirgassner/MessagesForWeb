Imports System.ComponentModel
Imports Microsoft.Web.WebView2.Core
Imports Microsoft.Toolkit.Uwp.Notifications

Public Class Main
    Dim AllowClose As Boolean = False

    Private Sub WV_NavigationCompleted(sender As Object, e As CoreWebView2NavigationCompletedEventArgs)
        Try
            If Text <> WV.CoreWebView2.DocumentTitle Then
                Text = WV.CoreWebView2.DocumentTitle
            End If
        Catch ex As Exception
            ' Handle exception (optional)
        End Try
    End Sub

    Private Sub SystemTrayIcon_MouseDoubleClick(sender As Object, e As MouseEventArgs) Handles SystemTrayIcon.MouseDoubleClick
        DoShow()
    End Sub

    Private Sub Main_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        If AllowClose = False AndAlso e.CloseReason = CloseReason.UserClosing Then
            e.Cancel = True
            SetPageVisible(False)
            ShowInTaskbar = False
            Visible = False
        End If
    End Sub
    Protected Overrides Sub WndProc(ByRef m As Message)
        If m.Msg = Hotkey.WM_HOTKEY Then
            Hotkey.handleHotKeyEvent(m.WParam, Me)
            Return
        End If
        If m.Msg = &H112 AndAlso (m.WParam.ToInt32() And &HFFF0) = &HF100 Then
            Return
        End If
        MyBase.WndProc(m)
    End Sub

    Private Sub ExitToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ExitToolStripMenuItem.Click
        AllowClose = True
        Close()
    End Sub

    Private Sub ShowToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ShowToolStripMenuItem.Click
        DoShow()
    End Sub

    Private Sub Startup_Tick(sender As Object, e As EventArgs) Handles Startup.Tick
        Startup.Enabled = False
        Dim s() As String = Environment.GetCommandLineArgs()
        For i = 1 To s.Length - 1
            If LCase(s(i)) = "-startup" Then
                Close()
            End If
        Next
        Opacity = 100
    End Sub

    Private Sub RestartToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles RestartToolStripMenuItem.Click
        Try
            Dim p As New Process()
            p.StartInfo.FileName = "cmd"
            p.StartInfo.Arguments = $"/C timeout /t 3 && start """" ""{Application.ExecutablePath}"""
            p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden
            p.Start()
        Catch ex As Exception
            ' Handle exception (optional)
        End Try

        Application.Exit()
    End Sub
    Public Sub DoShow()
        ShowInTaskbar = True
        Visible = True
        SetPageVisible(True)

        If WindowState = FormWindowState.Minimized Then
            WindowState = FormWindowState.Normal
        End If

        TopMost = True
        TopMost = False
        Activate()
    End Sub

    Private Sub WV_CoreWebView2InitializationCompleted(sender As Object, e As CoreWebView2InitializationCompletedEventArgs) Handles WV.CoreWebView2InitializationCompleted
        AddHandler WV.CoreWebView2.NewWindowRequested, AddressOf CoreWebView2_NewWindowRequested
        WV.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(PageVisibilityScript)
        AddHandler WV.CoreWebView2.NavigationCompleted, Sub(s, a) ApplyPageVisibility()
        If Not Visible Then SetPageVisible(False)
        AddHandler WV.CoreWebView2.NotificationReceived, AddressOf CoreWebView2_NotificationReceived
        AddHandler WV.CoreWebView2.PermissionRequested, AddressOf CoreWebView2_PermissionRequested
        ' Google Messages shows alerts via the service worker (persistent notifications), which WebView2
        ' does not pass to NotificationReceived. Turn them into page notifications so they are handled above.
        Dim notificationFix As String = "(function(){try{if(!window.ServiceWorkerRegistration||window.__mfwPatched)return;window.__mfwPatched=true;ServiceWorkerRegistration.prototype.showNotification=function(t,o){try{var c={};o=o||{};for(var k in o){if(k!=='actions')c[k]=o[k];}new Notification(t,c);}catch(e){}return Promise.resolve();};}catch(e){}})();"
        WV.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(notificationFix)
        WV.CoreWebView2.ExecuteScriptAsync(notificationFix)
    End Sub

    Private Sub CoreWebView2_NewWindowRequested(sender As Object, e As CoreWebView2NewWindowRequestedEventArgs)
        e.Handled = True

        Try
            ' Open only web links in the default browser
            Dim target As New Uri(e.Uri)
            If target.Scheme <> Uri.UriSchemeHttp AndAlso target.Scheme <> Uri.UriSchemeHttps Then Return
            Dim p As New Process()
            p.StartInfo.UseShellExecute = True
            p.StartInfo.FileName = e.Uri
            p.StartInfo.WindowStyle = ProcessWindowStyle.Normal
            p.Start()
        Catch ex As Exception
            ' Handle exception (optional)
        End Try
    End Sub

    ' Show web notifications as real Windows notifications instead of WebView2's own popup.
    ' ToastNotificationManagerCompat registers the app with Windows, so that clicks on a notification reach the app.
    ' App ID of this app. The installer puts the same ID on the Start Menu shortcut (see installer\MessagesForWeb.iss).
    Public Const ToastAppId As String = "DvirGassner.MessagesForWeb"
    Private LastNotification As CoreWebView2Notification
    Private ToastCounter As Integer = 0
    Private ReadOnly PendingNotifications As New Dictionary(Of Integer, CoreWebView2Notification)

    Private Sub InitToasts()
        Try
            AddHandler ToastNotificationManagerCompat.OnActivated, AddressOf Toast_Activated
        Catch ex As Exception
            ' Handle exception (optional)
        End Try
    End Sub

    ' Runs on a background thread when a Windows notification is clicked
    Private Sub Toast_Activated(e As ToastNotificationActivatedEventArgsCompat)
        Dim id As Integer = 0
        Try
            Dim args As ToastArguments = ToastArguments.Parse(e.Argument)
            Dim s As String = Nothing
            If args.TryGetValue("id", s) Then Integer.TryParse(s, id)
        Catch ex As Exception
            ' Handle exception (optional)
        End Try
        BeginInvoke(Sub() ToastClicked(id))
    End Sub

    Private Sub ToastClicked(id As Integer)
        Dim clicked As CoreWebView2Notification = Nothing
        If PendingNotifications.TryGetValue(id, clicked) Then PendingNotifications.Remove(id)
        If clicked IsNot Nothing Then OnNotificationClicked(clicked) Else DoShow()
    End Sub

    ' Allow notifications from Google Messages without asking, so that incoming messages can be shown
    Private Sub CoreWebView2_PermissionRequested(sender As Object, e As CoreWebView2PermissionRequestedEventArgs)
        Try
            If e.PermissionKind = CoreWebView2PermissionKind.Notifications AndAlso
               String.Equals(New Uri(e.Uri).Host, "messages.google.com", StringComparison.OrdinalIgnoreCase) Then
                e.State = CoreWebView2PermissionState.Allow
            End If
        Catch ex As Exception
            ' Handle exception (optional)
        End Try
    End Sub

    Private Sub CoreWebView2_NotificationReceived(sender As Object, e As CoreWebView2NotificationReceivedEventArgs)
        e.Handled = True
        Dim n As CoreWebView2Notification = e.Notification
        LastNotification = n
        Dim title As String = If(String.IsNullOrWhiteSpace(n.Title), "Messages", n.Title)
        Dim body As String = If(n.Body, "")
        Try
            ToastCounter += 1
            Dim toastId As Integer = ToastCounter
            If PendingNotifications.Count > 50 Then PendingNotifications.Clear()
            PendingNotifications(toastId) = n
            Dim builder As ToastContentBuilder = New ToastContentBuilder().AddArgument("id", toastId.ToString()).AddText(title)
            If Not String.IsNullOrWhiteSpace(body) Then builder.AddText(body)
            builder.Show()
        Catch ex As Exception
            ' Fall back to a tray balloon if the Windows notification fails
            Try
                SystemTrayIcon.ShowBalloonTip(5000, title, If(String.IsNullOrWhiteSpace(body), " ", body), ToolTipIcon.None)
            Catch ex2 As Exception
            End Try
        End Try
        Try
            n.ReportShown()
        Catch ex As Exception
        End Try
    End Sub

    Private Sub OnNotificationClicked(n As CoreWebView2Notification)
        DoShow()
        Try
            n.ReportClicked()
        Catch ex As Exception
            ' Handle exception (optional)
        End Try
    End Sub

    Private Sub SystemTrayIcon_BalloonTipClicked(sender As Object, e As EventArgs) Handles SystemTrayIcon.BalloonTipClicked
        If LastNotification IsNot Nothing Then OnNotificationClicked(LastNotification) Else DoShow()
    End Sub

    ' Called when the app is started again while it is already running (for example from the Start Menu)
    Public Sub HandleNextInstance(args As IEnumerable(Of String))
        For Each a As String In args
            If LCase(a) = "-startup" Then Return
        Next
        DoShow()
    End Sub

    ' Google Messages sends no notification while it thinks the user is looking at the page.
    ' WebView2 keeps reporting the page as visible and focused when the window is hidden in the tray,
    ' so the page is told about it by script as well.
    Private PageHidden As Boolean = False
    Private Const PageVisibilityScript As String = "(function(){if(window.__mfwVis)return;var h=false;var of=document.hasFocus.bind(document);Object.defineProperty(document,'visibilityState',{configurable:true,get:function(){return h?'hidden':'visible';}});Object.defineProperty(document,'hidden',{configurable:true,get:function(){return h;}});document.hasFocus=function(){return !h&&of();};window.__mfwVis=function(v){v=!!v;if(h===v)return;h=v;document.dispatchEvent(new Event('visibilitychange'));window.dispatchEvent(new Event(h?'blur':'focus'));};})();"

    Private Sub SetPageVisible(pageVisible As Boolean)
        PageHidden = Not pageVisible
        Try
            If WV IsNot Nothing Then WV.Visible = pageVisible
        Catch ex As Exception
            ' Handle exception (optional)
        End Try
        ApplyPageVisibility()
    End Sub

    Private Sub ApplyPageVisibility()
        Try
            If WV IsNot Nothing AndAlso WV.CoreWebView2 IsNot Nothing Then
                WV.CoreWebView2.ExecuteScriptAsync(PageVisibilityScript & "window.__mfwVis(" & If(PageHidden OrElse AlwaysNotify, "true", "false") & ");")
            End If
        Catch ex As Exception
            ' Handle exception (optional)
        End Try
    End Sub

    ' Option in the tray menu: show notifications also while the window is open.
    ' When it is on, the page is always told that it is hidden.
    Private Initialized As Boolean = False
    Private AlwaysNotify As Boolean = True
    Private Const SettingsKey As String = "Software\MessagesForWeb"

    Private Sub InitAlwaysNotify()
        Try
            Using k = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(SettingsKey)
                If k IsNot Nothing Then AlwaysNotify = (CInt(k.GetValue("AlwaysNotify", 1)) <> 0)
            End Using
            Dim item As New ToolStripMenuItem("Notify also when the window is open") With {.CheckOnClick = True, .Checked = AlwaysNotify}
            AddHandler item.CheckedChanged, AddressOf AlwaysNotify_CheckedChanged
            Dim menu As ContextMenuStrip = SystemTrayIcon.ContextMenuStrip
            If menu IsNot Nothing Then menu.Items.Insert(Math.Max(0, menu.Items.Count - 1), item)
        Catch ex As Exception
            ' Handle exception (optional)
        End Try
    End Sub

    Private Sub AlwaysNotify_CheckedChanged(sender As Object, e As EventArgs)
        AlwaysNotify = DirectCast(sender, ToolStripMenuItem).Checked
        Try
            Using k = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(SettingsKey)
                k.SetValue("AlwaysNotify", If(AlwaysNotify, 1, 0), Microsoft.Win32.RegistryValueKind.DWord)
            End Using
        Catch ex As Exception
            ' Handle exception (optional)
        End Try
        ApplyPageVisibility()
    End Sub

    Private Sub Main_Resize(sender As Object, e As EventArgs) Handles Me.Resize
        If Visible Then SetPageVisible(WindowState <> FormWindowState.Minimized)
    End Sub

    Private Sub Main_HandleCreated(sender As Object, e As EventArgs) Handles Me.HandleCreated
        Hotkey.registerHotkey(Me, "m", Hotkey.KeyModifier.Control Or Hotkey.KeyModifier.Alt)
        If Not Initialized Then
            ' The window handle is created again when the window moves between the taskbar and the tray
            Initialized = True
            InitToasts()
            InitAlwaysNotify()
        End If
    End Sub

    Private Sub Main_HandleDestroyed(sender As Object, e As EventArgs) Handles Me.HandleDestroyed
        Hotkey.unregisterHotkeys(Me)
    End Sub
End Class

Public Class Hotkey

#Region "Declarations - WinAPI, Hotkey constant and Modifier Enum"

    ''' <summary>
    '''     Declaration of winAPI function wrappers. The winAPI functions are used to register / unregister a hotkey
    ''' </summary>
    Private Declare Function RegisterHotKey Lib "user32" _
        (hwnd As IntPtr, id As Integer, fsModifiers As Integer, vk As Integer) As Integer

    Private Declare Function UnregisterHotKey Lib "user32" (hwnd As IntPtr, id As Integer) As Integer

    Public Const WM_HOTKEY As Integer = &H312

    Enum KeyModifier
        None = 0
        Alt = &H1
        Control = &H2
        Shift = &H4
        Winkey = &H8
    End Enum _
    'This enum is just to make it easier to call the registerHotKey function: The modifier integer codes are replaced by a friendly "Alt","Shift" etc.

#End Region

#Region "Hotkey registration, unregistration and handling"

    Public Shared Sub registerHotkey(ByRef sourceForm As Form, triggerKey As String, modifier As KeyModifier)
        Dim val As Integer
        If triggerKey = "ESC" Then
            val = Keys.Escape
        Else
            val = Asc(triggerKey.ToUpper)
        End If
        RegisterHotKey(sourceForm.Handle, 1, modifier, val)
    End Sub

    Public Shared Sub unregisterHotkeys(ByRef sourceForm As Form)
        UnregisterHotKey(sourceForm.Handle, 1)  'Remember to call unregisterHotkeys() when closing your application.
    End Sub

    Public Shared Sub handleHotKeyEvent(hotkeyID As IntPtr, targetForm As Main)
        targetForm.DoShow()
    End Sub

#End Region
End Class