rd /s /q "../ReleaseBinary"
"%DevEnvDir%devenv.exe" "../Codeer.Friendly.Windows/Codeer.Friendly.Windows.sln" /rebuild Release
"%DevEnvDir%devenv.exe" "../Codeer.Friendly.Windows/Codeer.Friendly.Windows.sln" /rebuild Release-Eng
nuget pack friendly.windows.nuspec