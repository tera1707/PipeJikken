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

            Debug.WriteLine("-----実験開始-----");

            var pipeName = "MyPipe1";

            // 受信Pipeサーバー立ち上げ
            _ = pipe.CreateServerAsync(pipeName, ((recvString) => { }), _cancelServer.Token);

            // 送信
            await pipe.CreateClientAsync(pipeName, "送る文字列１");
            await Task.Delay(2000);
            await pipe.CreateClientAsync(pipeName, "送る文字列２");
            await Task.Delay(2000);

            // キャンセル(サーバーを終了させる)
            Debug.WriteLine($"  Main：パイプサーバーをキャンセルします");
            _cancelServer.Cancel();

            await Task.Delay(1000);

            // キャンセルした後の送信ができないことを見る
            try
            {
                await pipe.CreateClientAsync(pipeName, "送る文字列３");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"送信失敗 : {ex.GetType()} {ex.Message}");
            }

            Debug.WriteLine($"-----実験終了-----");
        }
    }
}