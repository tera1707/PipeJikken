using PipeJikken;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace TestProject
{
    [TestClass]
    public class UnitTest
    {
        string pipeName = "MyPipe1";

        [TestMethod]
        [Description("���M����������Ǝ�M���������񂪈�v���邱�Ƃ̊m�F")]
        public async Task TestMethod1()
        {
            var _cancelServer = new CancellationTokenSource();
            var _cancelClient = new CancellationTokenSource();

            var sendString = "���镶����";
            var recvString = string.Empty;

            using (var pipe = new PipeConnect())
            {
                Debug.WriteLine("-----�����J�n-----");

                // ��MPipe�T�[�o�[�����グ
                _ = pipe.CreateServerAsync(pipeName, OnRecv, _cancelServer.Token);

                // ���M
                await pipe.CreateClientAsync(pipeName, sendString);
                await Task.Delay(2000);

                Assert.AreEqual(sendString, recvString);
            }

            void OnRecv(string s)
            {
                recvString = s;
            }
        }

        [TestMethod]
        [Description("��M�^�X�N���L�����Z���ł��邱�Ƃ̊m�F")]
        public async Task TestMethod2()
        {
            var _cancelServer = new CancellationTokenSource();
            var _cancelClient = new CancellationTokenSource();

            var sendString = "���镶����";
            var recvString = string.Empty;

            using (var pipe = new PipeConnect())
            {
                var recvTask = pipe.CreateServerAsync(pipeName, ((recvString) => { }), _cancelServer.Token);

                await pipe.CreateClientAsync(pipeName, sendString);

                await Task.Delay(1000);

                _cancelServer.Cancel();

                await Task.Delay(1000);

                // �L�����Z��������̑��M���ł��Ȃ����Ƃ�����
                await Assert.ThrowsExceptionAsync<TimeoutException>(() => pipe.CreateClientAsync(pipeName, "AAA"));

                //recvTask�̗�O��OperationCancelledException�ł��邱�Ƃ̊m�F
                try
                {
                    await Task.WhenAll(recvTask);
                }
                catch (OperationCanceledException)
                {
                    Assert.AreEqual(true, recvTask.IsCanceled);
                }
            }
        }

        [TestMethod]
        [Description("Dispose�Ŏ�M�^�X�N���L�����Z������邱�Ƃ̊m�F")]
        public async Task TestMethod3()
        {
            var _cancelServer = new CancellationTokenSource();
            var _cancelClient = new CancellationTokenSource();

            var sendString = "���镶����";
            var recvString = string.Empty;

            var pipe = new PipeConnect();
            
            var recvTask = pipe.CreateServerAsync(pipeName, ((recvString) => { }), _cancelServer.Token);

            await pipe.CreateClientAsync(pipeName, sendString);
            
            await Task.Delay(1000);

            pipe.Dispose();

            await Task.Delay(1000);

            // �L�����Z��������̑��M���ł��Ȃ����Ƃ�����
            await Assert.ThrowsExceptionAsync<TimeoutException>(() => pipe.CreateClientAsync(pipeName, "AAA"));

            //recvTask�̗�O��OperationCancelledException�ł��邱�Ƃ̊m�F
            try
            {
                await Task.WhenAll(recvTask);
            }
            catch (OperationCanceledException)
            {
                Assert.AreEqual(true, recvTask.IsCanceled);
            }
        }


        [TestMethod]
        [Description("�A�����đ��M�����ꍇ�Ɏ󂯎��邩�m�F")]
        public async Task TestMethod4()
        {
            var recvList = new ConcurrentBag<string>();
            var _cancelServer = new CancellationTokenSource();
            var _cancelClient = new CancellationTokenSource();

            var sendString = "���镶����";
            var recvString = string.Empty;

            using var pipe = new PipeConnect();

            // ��MPipe�T�[�o�[�����グ
            _ = pipe.CreateServerAsync(pipeName, OnRecv, _cancelServer.Token);

            for (int i = 0; i < 100; i++)
            {
                // ���M
                await pipe.CreateClientAsync(pipeName, sendString + i.ToString());
            }

            for (int i = 0; i < 100; i++)
            {
                var s = sendString + i.ToString();// �������͂��̕�����
                if (!recvList.Contains(s))
                {
                    Assert.Fail("�����������񂪁A��M�ł��Ă��܂���ł��� �� " + s);
                }
            }

            Debug.WriteLine("OK");

            void OnRecv(string s)
            {
                recvList.Add(s);
            }
        }
    }
}