Imports System.Runtime.InteropServices
Imports Microsoft.VisualBasic.ApplicationServices

Namespace My
    Partial Friend Class MyApplication
        <DllImport("shell32.dll", CharSet:=CharSet.Unicode)>
        Private Shared Function SetCurrentProcessExplicitAppUserModelID(appId As String) As Integer
        End Function

        ' Give the process the same app ID as the Start Menu shortcut, so Windows links its notifications to the app
        Private Sub MyApplication_Startup(sender As Object, e As StartupEventArgs) Handles Me.Startup
            Try
                SetCurrentProcessExplicitAppUserModelID(Global.MessagesForWeb.Main.ToastAppId)
            Catch ex As Exception
            End Try
        End Sub

        ' The app was started again while it is already running (for example from the Start Menu)
        Private Sub MyApplication_StartupNextInstance(sender As Object, e As StartupNextInstanceEventArgs) Handles Me.StartupNextInstance
            e.BringToForeground = False
            Dim f As Main = TryCast(Me.MainForm, Main)
            If f IsNot Nothing Then f.HandleNextInstance(e.CommandLine)
        End Sub
    End Class
End Namespace
