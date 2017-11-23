rd /s /q "../ReleaseBinary"
"C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\Common7\IDE\devenv.exe" "../Codeer.Friendly.Windows/Codeer.Friendly.Windows.sln" /rebuild Release
"C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\Common7\IDE\devenv.exe" "../Codeer.Friendly.Windows/Codeer.Friendly.Windows.sln" /rebuild Release-Eng
nuget pack friendly.windows.nuspec