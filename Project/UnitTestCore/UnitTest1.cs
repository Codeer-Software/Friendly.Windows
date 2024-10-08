using Codeer.Friendly.Windows;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Serialization;
using MessagePack;
using Codeer.Friendly.Dynamic;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using Codeer.Friendly.Windows.Inside.CopyDataProtocol;

namespace UnitTestCore
{
    public class IntPtrFormatter : IMessagePackFormatter<IntPtr>
    {
        // シリアライズの処理
        public void Serialize(ref MessagePackWriter writer, IntPtr value, MessagePackSerializerOptions options)
        {
            // IntPtr を long に変換して書き込み
            writer.Write(value.ToInt64());
        }

        // デシリアライズの処理
        public IntPtr Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            // long から IntPtr に変換して読み込み
            return new IntPtr(reader.ReadInt64());
        }
    }


    public class CustomSerializer : ICustomSerializer
    {
        MessagePackSerializerOptions customOptions = MessagePackSerializerOptions
            .Standard
            .WithResolver(
                CompositeResolver.Create(
                    new IMessagePackFormatter[] { new IntPtrFormatter() },  // カスタムフォーマッタを追加
                    new IFormatterResolver[] { TypelessContractlessStandardResolver.Instance } // Typeless を使用
                )
            );

        public object? Deserialize(byte[] bin)
            => MessagePackSerializer.Typeless.Deserialize(bin, customOptions);

        public Assembly[] GetRequiredAssemblies() => [GetType().Assembly, typeof(MessagePackSerializer).Assembly];

        public byte[] Serialize(object obj)
            => MessagePackSerializer.Typeless.Serialize(obj, customOptions);
    }


    public class Tests
    {
        WindowsAppFriend _app;

        [SetUp]
        public void Setup()
        {
            var targetExePath = Path.GetFullPath(GetType().Assembly.Location + @"..\..\..\..\..\..\TestTargetCore8\TestTargetCore8\bin\Debug\net8.0-windows\TestTargetCore8.exe");
            var targetApp = Process.Start(targetExePath);
            WindowsAppFriend.SetCustomSerializer<CustomSerializer>();
            _app = new WindowsAppFriend(targetApp);
        }

        [TearDown]
        public void TestCleanup()
        {
            _app.Dispose();
            Process.GetProcessById(_app.ProcessId).Kill();
        }
        
        [Test]
        public void Test1()
        {
            //var targetApp = Process.GetProcessesByName("TestTargetCore8").First();
            //var _app = new WindowsAppFriend(targetApp);
            int count = _app.Type<Application>().OpenForms.Count;
            var form = _app.Type<Application>().OpenForms[0];
            string result = form.Test(1, "abc", form);
            Assert.That(result, Is.EqualTo("1abcTestTargetCore8.Form1, Text: Form1"));
        }

        [Test]
        public void Test2()
        {
            
            var customOptions = MessagePackSerializerOptions
                .Standard
                .WithResolver(
                    CompositeResolver.Create(
                        new IMessagePackFormatter[] { new IntPtrFormatter() },  // カスタムフォーマッタを追加
                        new IFormatterResolver[] { TypelessContractlessStandardResolver.Instance } // Typeless を使用
                    )
                );
            
            var obj = new CopyDataProtocolInfo();
            MessagePackSerializer.Typeless.Serialize(obj, customOptions);
        }
    }
}