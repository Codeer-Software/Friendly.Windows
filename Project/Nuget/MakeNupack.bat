rd /s /q "../ReleaseBinary"
"C:\Program Files\Microsoft Visual Studio\2022\Professional\Common7\IDE\devenv.exe" "../Codeer.Friendly.Windows/Codeer.Friendly.Windows.sln" /rebuild Release
"C:\Program Files\Microsoft Visual Studio\2022\Professional\Common7\IDE\devenv.exe" "../Codeer.Friendly.Windows/Codeer.Friendly.Windows.sln" /rebuild Release-Eng
nuget pack friendly.windows.nuspec