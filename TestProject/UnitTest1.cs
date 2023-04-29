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
        [Description("送信した文字列と受信した文字列が一致することの確認")]
        public async Task TestMethod1()
        {
            var _cancelServer = new CancellationTokenSource();
            var _cancelClient = new CancellationTokenSource();

            var sendString = "送る文字列";
            var recvString = string.Empty;

            using (var pipe = new PipeConnect())
            {
                Debug.WriteLine("-----実験開始-----");

                // 受信Pipeサーバー立ち上げ
                _ = pipe.CreateServerAsync(pipeName, OnRecv, _cancelServer.Token);

                // 送信
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
        [Description("受信タスクがキャンセルできることの確認")]
        public async Task TestMethod2()
        {
            var _cancelServer = new CancellationTokenSource();
            var _cancelClient = new CancellationTokenSource();

            var sendString = "送る文字列";
            var recvString = string.Empty;

            using (var pipe = new PipeConnect())
            {
                var recvTask = pipe.CreateServerAsync(pipeName, ((recvString) => { }), _cancelServer.Token);

                await pipe.CreateClientAsync(pipeName, sendString);

                await Task.Delay(1000);

                _cancelServer.Cancel();

                await Task.Delay(1000);

                // キャンセルした後の送信ができないことを見る
                await Assert.ThrowsExceptionAsync<TimeoutException>(() => pipe.CreateClientAsync(pipeName, "AAA"));

                //recvTaskの例外がOperationCancelledExceptionであることの確認
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
        [Description("Disposeで受信タスクがキャンセルされることの確認")]
        public async Task TestMethod3()
        {
            var _cancelServer = new CancellationTokenSource();
            var _cancelClient = new CancellationTokenSource();

            var sendString = "送る文字列";
            var recvString = string.Empty;

            var pipe = new PipeConnect();
            
            var recvTask = pipe.CreateServerAsync(pipeName, ((recvString) => { }), _cancelServer.Token);

            await pipe.CreateClientAsync(pipeName, sendString);
            
            await Task.Delay(1000);

            pipe.Dispose();

            await Task.Delay(1000);

            // キャンセルした後の送信ができないことを見る
            await Assert.ThrowsExceptionAsync<TimeoutException>(() => pipe.CreateClientAsync(pipeName, "AAA"));

            //recvTaskの例外がOperationCancelledExceptionであることの確認
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
        [Description("連続して送信した場合に受け取れるか確認")]
        public async Task TestMethod4()
        {
            var recvList = new ConcurrentBag<string>();
            var _cancelServer = new CancellationTokenSource();
            var _cancelClient = new CancellationTokenSource();

            var sendString = "送る文字列";
            var recvString = string.Empty;

            using var pipe = new PipeConnect();

            // 受信Pipeサーバー立ち上げ
            _ = pipe.CreateServerAsync(pipeName, OnRecv, _cancelServer.Token);

            for (int i = 0; i < 100; i++)
            {
                // 送信
                await pipe.CreateClientAsync(pipeName, sendString + i.ToString());
            }

            for (int i = 0; i < 100; i++)
            {
                var s = sendString + i.ToString();// 送ったはずの文字列
                if (!recvList.Contains(s))
                {
                    Assert.Fail("送った文字列が、受信できていませんでした → " + s);
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