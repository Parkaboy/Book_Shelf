Book shelf is an e-book management system, I have used Calibre for a while and it is AWESOME, but I wanted a beter looking interface and really didn't needed most of the features so I decided to develop my own way to manage my book files.

# how to install

To build the Windows installer, install the .NET 10 SDK and Inno Setup, then run these commands from the repository root:

```powershell
dotnet publish Book_Shelf.csproj -c Release -r win-x64 --self-contained true -o artifacts\publish\win-x64
& "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe" installer_script.iss
```

The installer is created as `Output\BookShelfSetup.exe`. It includes the complete self-contained publish output, so the target computer does not need the .NET runtime installed.

# how to use
