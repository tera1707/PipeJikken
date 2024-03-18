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

            var responseString = "応答文字列";
            var recvResponseString = string.Empty;

            var server = new PipeServer();
            var client = new PipeClient();

            // 受信Pipeサーバー立ち上げ
            server.Create(pipeName);
            _ = server.StartAsync( (async s =>
            {
                // 受信
                recvString = s;
                // クライアントへの応答を送信
                await server.SendString(responseString);
            }), _cancelServer.Token);

            // 送信クライアント立ち上げ
            _ = client.Create(pipeName);
            // 応答を送信
            await client.SendAsync(sendString, (recv =>
            {
                // 送信に対するサーバー応答を受信
                recvResponseString = recv;
            }));

            server.Dispose();
            client.Dispose();

            // clientが送信し、serverが受信できているのを確認
            Assert.AreEqual(sendString, recvString);

            // serverが応答し、clientが受信できていることを確認
            Assert.AreEqual(responseString, recvResponseString);
        }

        [TestMethod]
        [Description("受信タスクがキャンセルできることの確認")]
        public async Task TestMethod2()
        {
            var _cancelServer = new CancellationTokenSource();
            var _cancelClient = new CancellationTokenSource();

            var server = new PipeServer();
            var client = new PipeClient();

            // 受信Pipeサーバー立ち上げ
            server.Create(pipeName);
            var serverTask = server.StartAsync((s => { }), _cancelServer.Token);

            await Task.Delay(1000);

            // キャンセルする
            _cancelServer.Cancel();

            await Task.Delay(1000);

            // キャンセルした後の送信ができないことを見る
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => client.SendAsync("AAA"));

            try
            {
                await serverTask;
            }
            catch (OperationCanceledException oce)
            {
                Assert.AreEqual(true, serverTask.IsCanceled);
            }

            server.Dispose();
            client.Dispose();
        }

        [TestMethod]
        [Description("Disposeで受信タスクがキャンセルされることの確認")]
        public async Task TestMethod3()
        {
            var _cancelServer = new CancellationTokenSource();
            var _cancelClient = new CancellationTokenSource();

            var server = new PipeServer();
            var client = new PipeClient();

            // 受信Pipeサーバー立ち上げ
            server.Create(pipeName);
            var serverTask = server.StartAsync((s => { }), _cancelServer.Token);

            await Task.Delay(1000);

            // キャンセルする
            server.Dispose();

            await Task.Delay(1000);

            // キャンセルした後の送信ができないことを見る
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => client.SendAsync("AAA"));

            try
            {
                await serverTask;
            }
            catch (OperationCanceledException oce)
            {
                Assert.AreEqual(true, serverTask.IsCanceled);
            }

            //server.Dispose();
            client.Dispose();
        }


        [TestMethod]
        [Description("連続して送信した場合に受け取れるか確認")]
        public async Task TestMethod4()
        {
            var _cancelServer = new CancellationTokenSource();
            var _cancelClient = new CancellationTokenSource();

            var sendString = "送る文字列";
            var recvString = string.Empty;

            var responseString = "応答文字列";
            var recvResponseString = string.Empty;

            var recvDataList = new List<string>();
            var responseDataList = new List<string>();

            var serverRecvCount = 0;

            var server = new PipeServer();
            var client = new PipeClient();

            // 受信Pipeサーバー立ち上げ
            server.Create(pipeName);
            _ = server.StartAsync((async s =>
            {
                recvDataList.Add(s);

                await server.SendString(responseString + serverRecvCount++.ToString());
            }), _cancelServer.Token);

            await Task.Delay(1000);

            // 送信クライアント立ち上げ
            _ = client.Create(pipeName);

            for (int i = 0; i < 100; i++)
            {
                // 応答を送信
                await client.SendAsync(sendString + i.ToString(), (recv =>
                {
                    responseDataList.Add(recv);
                }));
            }

            // clientが送信し、serverが受信できているのを確認
            for (int i = 0; i < 100; i++)
            {
                Assert.AreEqual(true, recvDataList.Contains(sendString + i));
            }

            // clientが送信し、serverが受信できているのを確認
            for (int i = 0; i < 100; i++)
            {
                Assert.AreEqual(true, responseDataList.Contains(responseString + i));
            }

            server.Dispose();
            client.Dispose();
        }


        [TestMethod]
        [Description("サーバーが立ち上がっていないときにクライアントから送信")]
        public async Task TestMethod5()
        {
            var client = new PipeClient();
            await client.Create(pipeName);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await client.SendAsync("abc"));
            client.Dispose();
        }
    }
}