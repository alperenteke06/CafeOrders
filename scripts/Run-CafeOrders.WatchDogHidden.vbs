Option Explicit

Dim args, scriptPath, webUiUrl, apiAppPoolName, webUiAppPoolName, apiSiteName, webUiSiteName, logPath
Set args = WScript.Arguments

scriptPath = ReadArg(args, 0, "C:\Scripts\CafeOrders.WatchDog.ps1")
webUiUrl = ReadArg(args, 1, "http://192.168.1.104:5002/")
apiAppPoolName = ReadArg(args, 2, "CafeOrders.API")
webUiAppPoolName = ReadArg(args, 3, "CafeOrders.WebUI")
apiSiteName = ReadArg(args, 4, "CafeOrders.API")
webUiSiteName = ReadArg(args, 5, "CafeOrders.WebUI")
logPath = ReadArg(args, 6, "C:\Scripts\CafeOrders.WatchDog.log")

Dim powershellCommand
powershellCommand = "powershell.exe -ExecutionPolicy Bypass -NoProfile -WindowStyle Hidden -File " & Quote(scriptPath) & _
    " -WebUiUrl " & Quote(webUiUrl) & _
    " -ApiAppPoolName " & Quote(apiAppPoolName) & _
    " -WebUiAppPoolName " & Quote(webUiAppPoolName) & _
    " -ApiSiteName " & Quote(apiSiteName) & _
    " -WebUiSiteName " & Quote(webUiSiteName) & _
    " -LogPath " & Quote(logPath)

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
