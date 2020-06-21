Friendly.Windows
========

Friendly is a library for creating integration tests.<br>
It has the ability to manipulate other processes.<br>
It is currently designed for Windows Applications (**WinForms**, **WPF**, and **Win32**).<br>
The name Friendly is derived from the C++ friend class. <br>
Being friends gives you access to what you normally wouldn't be able to do.<br>

## Friendly support .NetCore.
Friendly can also operate .NetCore WinForms and WPF apps. But please write the test code in .Net Framework. Sorry.

## Features ...
### Invoke separate process's API.
It's like a selenium's javascript execution.<br>
All Methods, Properties and Fields can be called regardless of being public internal protected private.
### DLL injection.
It can inject .net assembly. And can execute inserted methods.

## Getting Started
Install Friendly.Windows from NuGet<br>

    PM> Install-Package Codeer.Friendly.Windows

https://www.nuget.org/packages/Codeer.Friendly.Windows/

## See Friendly ReadMe for details
https://github.com/Codeer-Software/Friendly

Initially, Friendly defined a common interface, and Friendly.Windows was positioned as a Windows application version of that, but in reality there are only Windows applications, so I decided to put the documents together.