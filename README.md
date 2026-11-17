# Messages for Web (on Windows)

Runs [Google Messages for web](https://messages.google.com/web) inside a native Windows application that lives in the system tray.

This is a fork of [asheroto/messagesforweb](https://github.com/asheroto/messagesforweb). It adds real Windows notifications and a standard installer.

## What is different in this fork

Changes made in November 2026:

- **Windows notifications.** An incoming message shows as a normal Windows notification and stays in the notification center. The original shows the small popup of the embedded browser, which Windows does not keep.
- **Click to open.** A click on a notification opens the app on that conversation.
- **Notifications while the window is open.** Google Messages sends no notification while it thinks you are looking at it. This fork can notify always. Turn this off with **Notify also when the window is open** in the tray menu.
- **No permission prompt.** Notifications from `messages.google.com` are allowed automatically.
- **Installer.** A per-user installer (no administrator rights needed) with an uninstaller and an option to start with Windows.
- **Other.** 64-bit build, sharp text on high-DPI screens, a normal window size at first start, and starting the app again shows the running window.

## Install

1. Download `MessagesForWeb-Setup-x.y.z.exe` from the [latest release](https://github.com/dvirgassner/messagesforweb/releases/latest).
2. Run it. The installer is not code-signed, so Windows SmartScreen may warn you. Choose **More info** > **Run anyway**.
3. Pair your phone: scan the QR code with the Google Messages app, or sign in with your Google account.

Requirements: Windows 10 or 11 (64-bit), .NET Framework 4.8 and the Microsoft Edge WebView2 Runtime. Windows 11 includes all of them.

## Use

- **X** hides the window to the tray. The app keeps running and keeps notifying.
- **Ctrl+Alt+M**, a double-click on the tray icon, or a click on a notification shows the window.
- Right-click the tray icon for **Show**, **Restart**, **Notify also when the window is open** and **Exit**.
- To start hidden in the tray, pass the `-startup` argument. The installer's "start with Windows" option does this for you.

```powershell
.\MessagesForWeb.exe -startup
```

## Privacy

- **No telemetry, no tracking, no ads, no analytics.** The app's own code makes no network requests and collects nothing.
- The app only opens `https://messages.google.com/web` in an embedded browser (Microsoft Edge WebView2) and passes its notifications to Windows.
- Your messages and your pairing stay between the embedded browser and Google, as in a normal browser. They are stored in the app's folder on your PC and are removed when you uninstall.
- Links you click in a message open in your default browser. Only `http` and `https` links are opened.
- The code is small and open. You can read it and build it yourself (see below).
- The Google Messages page and the WebView2 runtime are made by Google and Microsoft, and their own privacy terms apply to them.

## Build from source

You need Visual Studio 2022 (with the .NET desktop workload) and, for the installer, [Inno Setup 6](https://jrsoftware.org/isinfo.php).

```powershell
msbuild MessagesForWeb.sln -t:Restore,Build -p:Configuration=Release
iscc installer\MessagesForWeb.iss
```

The installer is written to `installer\output`.

Windows shows the notifications of a desktop app, and sends clicks on them to the app, only if the app's Start Menu shortcut carries the app ID and the toast activator ID. The installer creates this shortcut. If you run the built `.exe` without installing, notifications may not show.

## License and credits

- Licensed under the [GNU Affero General Public License v3.0](LICENSE), the same license as the original.
- Original app by [asheroto](https://github.com/asheroto). You can support the original author on [Ko-fi](https://ko-fi.com/asheroto).
- Third-party components and their licenses are listed in [THIRD-PARTY-NOTICES.txt](THIRD-PARTY-NOTICES.txt).
- This project is not affiliated with Google.