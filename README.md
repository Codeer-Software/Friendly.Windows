Friendly.Windows
======================
Friendly is a library for creating integration tests.
(The included tools can be useful, but these are only a bonus.)
It is currently designed for Windows Applications (WinForms, WPF, and Win32).
It can be used to start up a product process and run tests on it..
However, the way of operating the target program is different from conventional automated GUI tests (capture replay tool, etc.).

## Features ...
####Invoke other process's API.
It can invoke all methods, properties, and fields.
And it executes on a separate process.
It's like a selenium's javascript execution.
####DLL injection.
It can inject .net assembly. And can execute inserted methods.

## Getting Started
Install Friendly.Windows from NuGet

    PM> Install-Package Codeer.Friendly.Windows
## Movies
https://youtu.be/CK327YuI-bk?t=17<br>
https://youtu.be/xy7BvrrF8oE

## Simple sample
Here is some sample code to show how you can get started with Friendly

This is a perfect ordinary Windows Application that is manipulation target.
(There is no kind of trick.)
```cs  
using System.Windows.Forms;

namespace ProductProcess
{
    public partial class SampleForm : Form
    {
        int testValue;

        private void SetTestValue(int value)
        {
            Text = value.ToString();
            testValue = value;
        }
    }
}
```
This is a test application (using VSTest):
```cs  
using System;
using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Codeer.Friendly;
using Codeer.Friendly.Dynamic;
using Codeer.Friendly.Windows;

namespace TestProcess
{
    [TestClass]
    public class BasicSample
    {
        WindowsAppFriend _app;

        [TestInitialize]
        public void TestInitialize()
        {
            //attach to target process!
             _app = new WindowsAppFriend(Process.Start("ProductProcess.exe"));
        }

        [TestCleanup]
        public void TestCleanup()
        {
             Process process = Process.GetProcessById(_app.ProcessId);
            _app.Dispose();
             process.CloseMainWindow();
        }

        [TestMethod]
        public void TestSetValue()
        {
            //static method
            dynamic sampleForm = _app.Type<Application>().OpenForms[0];

            //instance method
            sampleForm.SetTestValue(5);

            //instance field
            int value = sampleForm.testValue;

            //instance property
            string text = sampleForm.Text;

            Assert.AreEqual(5, value);
            Assert.AreEqual("5", text);
        }
    }
}
```
#### Match the Processor Architecture. (x86 or x64)

The target and test processes must use the same processor architectue.
If you are using VSTest, you can set this by using the Visual Studio menus as shown below.
![Match the Processor Architecture](https://e1e82e8d-a-0cb309f2-s-sites.googlegroups.com/a/codeer.co.jp/english-home/test-automation/friendly-fundamental/CpuType.png?attachauth=ANoY7cprljI9CC8x0rTkRwfg5HBpKd0YCFHFC6qBaDgXmiO3vM_QgrB-0HANaGG8P4Oqw9io-zhGqJSCq9OOZC_4eZFD9sdVDJLBblfoNFznSoXhDYnyTVIS81ctl-rNBeWrMgciHQSCndb2YSFKaCOsVg_flygABUpVmTFrDqt7lZLhDG8vYrXAaRy2qzeJBD1nG5NMftXHcI0teetvoNZlwSWWUu6Lr6Y-fBXWvM49g-MrYsXiU2TPtG8XsCEHQQtu9ZdDPe0q&attredirects=0)

#### Using Statements
```cs  
using Codeer.Friendly;
using Codeer.Friendly.Dynamic;
using Codeer.Friendly.Windows;
```

#### Connection to Execution Thread

Attach using WindowsAppFriend.
Operations can be executed on the main window thread:
```cs  
public WindowsAppFriend(Process process);
```
Operation can also be executed on a specified window thread:
```cs  
public WindowsAppFriend(IntPtr windowHandle);
```
#### Invoking Static Operations（Any OK）
```cs  
dynamic sampleForm1 = _app.Type<Application>().OpenForms[0];

dynamic sampleForm2 = _app.Type(typeof(Application)).OpenForms[0];

dynamic sampleForm3 = _app.Type().System.Windows.Forms.Application.OpenForms[0];

dynamic sampleForm4 = _app.Type("System.Windows.Forms.Application").OpenForms[0];

dynamic sampleForm5 = _app.Type<Control>().FromHandle(handle);
```
#### Invokeing Instance Operations
```cs  
//method
sampleForm.SetTestValue(5);

//field
int value = sampleForm.testValue;

//property
string text = sampleForm.Text;
```
Variables are referenced from the target process.
You can access public and private members.

#### Instantiating New Objects
```cs  
dynamic listBox1 = _app.Type<ListBox>()();

dynamic listBox2 = _app.Type(typeof(ListBox))();

dynamic listBox3 = _app.Type().System.Windows.Forms.ListBox();

dynamic listBox4 = _app.Type("System.Windows.Forms.ListBox")();

dynamic list = _app.Type<List<int>>()(new int[]{1, 2, 3, 4, 5});
```

#### Rules for Arguments

You can use serializable objects and reference them in the target process.
If you use serializable objects, they will be serialized and a copy will be sent to the target process. 
// get SampleForm reference from the target process.
dynamic sampleForm = _app.Type<Application>().OpenForms[0];
```cs  
 // new instance in target process.
dynamic listBox = _app.Type<ListBox>()();

// serializable object
listBox.Location = new Point(10, 10); 

// serializable object
listBox.Items.Add("Item"); 

// reference to target process
sampleForm.Controls.Add(listBox); 
```

####Return Values
```cs  
// referenced object exists in target process' memory. 
dynamic reference = sampleForm.Text;

// when you perform a cast, it will be marshaled from the target process.
string text = (string)reference;
```

####Note the Casting Behavior
```cs  
// OK
string cast = (string)reference;

// OK
string substitution = reference;

// No good. Result is false.
bool isString = reference is string;

// No good. Result is null.
string textAs = reference as string;

// No good. Throws an exception.
string.IsNullOrEmpty(reference);

// OK
string.IsNullOrEmpty((string)reference);
```

####Special Casts
IEnumerable
```cs  
foreach (dynamic form in _app.Type<Application>().OpenForms)
{
    form.BackColor = Color.Pink;
}
```
AppVar
```cs  
dynamic sampleForm = _app.Type("System.Windows.Forms.Control").FromHandle(_process.MainWindowHandle);

AppVar appVar = sampleForm;
appVar["Text"]("abc");
```
AppVar is part of the old style interface.
You will need to use AppVar if you use the old interface or if you can't use the .NET framework 4.0.
The old style sample code is pending translation, but the code is in C#.
Please have a look [here](http://www.codeer.co.jp/AutoTest/friendly-basic) if you are interested.

Async
Friendly operations are executed synchronously.
But you can use the Async class to execute them asynchronously.
```cs  
// Async can be specified anywhere among the arguments.
Async async = new Async();
sampleForm.SetTestValue(async, 5);

// You can check whether it has completed.
if (async.IsCompleted)
{
    //・・・
}

// You can wait for it to complete.
async.WaitForCompletion();
```

Return Values
```cs  
// Invoke getter.
Async async = new Async();

// Text will obtain its value when the operation completes.
dynamic text = sampleForm.Text(async);

// When the operation finishes, the value will be available.
async.WaitForCompletion();
string textValue = (string)text;
```
####Copy() and Null()
```cs  
Dictionary<int, string> dic = new Dictionary<int, string>();
dic.Add(1, "1");

// Object is serialized and a copy will be sent to the target process 
dynamic dicInTarget = _app.Copy(dic);
            
// Null is useful for out arguments
dynamic value = _app.Null();
dicInTarget.TryGetValue(1, value);
Assert.AreEqual("1", (string)value);
```
####Dll injection.
```cs  
[TestMethod]
public void Test()
{
    dynamic mainWindow = app.Type<Application>().Current.Windows[0];
    dynamic button = mainWindow.button;

    //The code let tasrget process load current assembly.
    WindowsAppExpander.LoadAssembly(app, GetType().Assembly);

    //You can use class defined in current assembly.
    dynamic observer = app.Type<Observer>()(button);

    //Check click.
    button.OnClick();
    Assert.IsTrue((bool)observer.Clicked);
}

class Observer
{
    internal bool Clicked { get; set; }
    internal Observer(Button button)
    {
        button.Click += delegate { Clicked = true; };
    }
}

Native dll methods.
[TestMethod]
public void TestRect()
{
    WindowsAppExpander.LoadAssembly(_app, GetType().Assembly); 

    Process process = Process.GetProcessById(_app.ProcessId);
    _app.Type<BasicSample>().MoveWindow(process.MainWindowHandle, 0, 0, 200, 200, true);

    dynamic rectInTarget = _app.Type<RECT>()();
    _app.Type<BasicSample>().GetWindowRect(process.MainWindowHandle, rectInTarget); 
    RECT rect = (RECT)rectInTarget; 

    Assert.AreEqual(0, rect.left); 
    Assert.AreEqual(0, rect.top); 
    Assert.AreEqual(200, rect.right); 
    Assert.AreEqual(200, rect.bottom); 
}

[DllImport("User32.dll")]
static extern bool MoveWindow(IntPtr handle, int x, int y, int width, int height, bool redraw); 


[DllImport("user32.dll")]
[return: MarshalAs(UnmanagedType.Bool)] 
static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect); 

[Serializable]
[StructLayout(LayoutKind.Sequential)]
internal struct RECT
{
    public int left; 
    public int top; 
    public int right; 
    public int bottom; 
}
```
##Upper Librarys
![Upper Librarys](https://e1e82e8d-a-0cb309f2-s-sites.googlegroups.com/a/codeer.co.jp/english-home/test-automation/friendly-fundamental/All_libs.png?attachauth=ANoY7cp7pCVvvAD5KHiXYsseaYDG6-kja9x2OnjEvYHzL8odv57zkdeqjXDTYECh4G4aAkacOC3RuYEsXXKSBnwIRvHCSZPUVybpGU1VRwgXi7pGXSKsMYKcpNu8p0pviG8eIk6ig4Ed0c-z9nkiBWYmZJHljrMy9wafiNYugrHXfXZfhYhhgsC26qb_7Z0ADryOdODYFkRjkyORt3Z5EkzocluiBnUAWYt-DgnhZKwEwLe806U9njSRJwaLdvXLpM-4inhBlXYG&attredirects=0)
#We win 2nd place at MVP Showcase. Thank you!
http://blogs.msdn.com/b/mvpawardprogram/archive/2014/11/04/mvp-showcase-winners.aspx

