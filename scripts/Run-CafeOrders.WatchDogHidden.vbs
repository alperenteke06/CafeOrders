Option Explicit

Dim args, scriptPath, apiHealthUrl, webUiUrl, apiAppPoolName, webUiAppPoolName, apiSiteName, webUiSiteName, logPath, adminAudioAgentPath, serverNotifierPath
Set args = WScript.Arguments

scriptPath = ReadArg(args, 0, "C:\Scripts\CafeOrders.WatchDog.ps1")
apiHealthUrl = ReadArg(args, 1, "http://192.168.2.11:5001/api/v1/settings/app")
webUiUrl = ReadArg(args, 2, "http://192.168.2.11:5002/")
apiAppPoolName = ReadArg(args, 3, "CafeOrders.API")
webUiAppPoolName = ReadArg(args, 4, "CafeOrders.WebUI")
apiSiteName = ReadArg(args, 5, "CafeOrders.API")
webUiSiteName = ReadArg(args, 6, "CafeOrders.WebUI")
logPath = ReadArg(args, 7, "C:\Scripts\CafeOrders.WatchDog.log")
adminAudioAgentPath = ReadArg(args, 8, "C:\AdminAudioAgent\CafeOrders.AdminAudioAgent.exe")
serverNotifierPath = ReadArg(args, 9, "C:\ServerNotifier\CafeOrders.ServerNotifier.exe")

Dim powershellCommand
powershellCommand = "powershell.exe -ExecutionPolicy Bypass -NoProfile -WindowStyle Hidden -File " & Quote(scriptPath) & _
    " -ApiHealthUrl " & Quote(apiHealthUrl) & _
    " -WebUiUrl " & Quote(webUiUrl) & _
    " -ApiAppPoolName " & Quote(apiAppPoolName) & _
    " -WebUiAppPoolName " & Quote(webUiAppPoolName) & _
    " -ApiSiteName " & Quote(apiSiteName) & _
    " -WebUiSiteName " & Quote(webUiSiteName) & _
    " -LogPath " & Quote(logPath) & _
    " -AdminAudioAgentPath " & Quote(adminAudioAgentPath) & _
    " -ServerNotifierPath " & Quote(serverNotifierPath)

Dim shell
Set shell = CreateObject("WScript.Shell")
shell.Run powershellCommand, 0, True

Function ReadArg(argumentList, index, fallback)
    If argumentList.Count > index Then
        ReadArg = argumentList(index)
    Else
        ReadArg = fallback
    End If
End Function

Function Quote(value)
    Quote = """" & Replace(CStr(value), """", """""") & """"
End Function
