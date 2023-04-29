using PipeJikken;
using System.Diagnostics;

namespace TestProject
{
    [TestClass]
    public class UnitTest
    {
        [TestMethod]
        public async Task TestMethod1()
        {
            var _cancelServer = new CancellationTokenSource();
            var _cancelClient = new CancellationTokenSource();

            var pipe = new PipeConnect();

            Debug.WriteLine("-----�����J�n-----");

            var pipeName = "MyPipe1";

            // ��MPipe�T�[�o�[�����グ
            _ = pipe.CreateServerAsync(pipeName, ((recvString) => { }), _cancelServer.Token);

            // ���M
            await pipe.CreateClientAsync(pipeName, "���镶����P");
            await Task.Delay(2000);
            await pipe.CreateClientAsync(pipeName, "���镶����Q");
            await Task.Delay(2000);

            // �L�����Z��(�T�[�o�[���I��������)
            Debug.WriteLine($"  Main�F�p�C�v�T�[�o�[���L�����Z�����܂�");
            _cancelServer.Cancel();

            await Task.Delay(1000);

            // �L�����Z��������̑��M���ł��Ȃ����Ƃ�����
            try
            {
                await pipe.CreateClientAsync(pipeName, "���镶����R");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"���M���s : {ex.GetType()} {ex.Message}");
            }

            Debug.WriteLine($"-----�����I��-----");
        }
    }
}