using Codeer.Friendly.Windows;
using System;
using System.Diagnostics;
using System.IO;

namespace Attachx86
{
    class Program
    {
        static void Main(string[] args)
        {
            var app = new WindowsAppFriend(Process.GetProcessById(int.Parse(args[0])));
            var bin = app.HandOverResources(int.Parse(args[1]));
            File.WriteAllBytes(args[2], bin);
        }
    }
}
