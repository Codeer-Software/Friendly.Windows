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
        // �V���A���C�Y�̏���
        public void Serialize(ref MessagePackWriter writer, IntPtr value, MessagePackSerializerOptions options)
        {
            // IntPtr �� long �ɕϊ����ď�������
            writer.Write(value.ToInt64());
        }

        // �f�V���A���C�Y�̏���
        public IntPtr Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            // long ���� IntPtr �ɕϊ����ēǂݍ���
            return new IntPtr(reader.ReadInt64());
        }
    }


    public class CustomSerializer : ICustomSerializer
    {

        MessagePackSerializerOptions customOptions = MessagePackSerializerOptions
            .Standard
            .WithResolver(
                CompositeResolver.Create(
                    new IMessagePackFormatter[] { new IntPtrFormatter() },  // �J�X�^���t�H�[�}�b�^��ǉ�
                    new IFormatterResolver[] { TypelessContractlessStandardResolver.Instance } // Typeless ���g�p
                )
            );


        public object Deserialize(byte[] bin)
#pragma warning disable CS8603 // Null �Q�Ɩ߂�l�ł���\��������܂��B
            => MessagePackSerializer.Typeless.Deserialize(bin, customOptions);
#pragma warning restore CS8603 // Null �Q�Ɩ߂�l�ł���\��������܂��B

        public Assembly[] GetRequiredAssemblies() => [GetType().Assembly, typeof(MessagePackSerializer).Assembly];

        public byte[] Serialize(object obj)
            => MessagePackSerializer.Typeless.Serialize(obj, customOptions);
    }


    public class Tests
    {
        // WindowsAppFriend _app;

        /*
        [SetUp]
        public void Setup()
        {
            var targetExePath = Path.GetFullPath(GetType().Assembly.Location + @"..\..\..\..\..\..\TestTargetCore8\TestTargetCore8\bin\Debug\net8.0-windows\TestTargetCore8.exe");
            var targetApp = Process.Start(targetExePath);
            _app = new WindowsAppFriend(targetApp);
        }

        [TearDown]
        public void TestCleanup()
        {
            _app.Dispose();
            Process.GetProcessById(_app.ProcessId).Kill();
        }
        */
        [Test]
        public void Test1()
        {
            var targetApp = Process.GetProcessesByName("TestTargetCore8").First();
            var _app = new WindowsAppFriend(targetApp);
            int c = _app.Type<Application>().OpenForms.Count;

        }

        [Test]
        public void Test2()
        {
            
            var customOptions = MessagePackSerializerOptions
                .Standard
                .WithResolver(
                    CompositeResolver.Create(
                        new IMessagePackFormatter[] { new IntPtrFormatter() },  // �J�X�^���t�H�[�}�b�^��ǉ�
                        new IFormatterResolver[] { TypelessContractlessStandardResolver.Instance } // Typeless ���g�p
                    )
                );
            
            var obj = new CopyDataProtocolInfo();
            MessagePackSerializer.Typeless.Serialize(obj, customOptions);
        }
    }
}