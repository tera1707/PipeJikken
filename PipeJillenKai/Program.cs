using PipeJikken;
using System.Diagnostics;
using System.IO.Pipes;
using System.Security.Principal;

namespace PipeJikkenKai
{
    internal class Program
    {
        private static CancellationTokenSource? _cancelServer;

        static async Task Main(string[] args)
        {
            _cancelServer = new CancellationTokenSource();

            using var pipe = new PipeConnect();

            ConsoleWriteLine("-----実験開始-----");

            var pipeName = "MyPipe1";

            // 受信Pipeサーバー立ち上げ

            _ = pipe.CreateServerAsync(pipeName, ((recvString) => { }), _cancelServer.Token);

            // 送信
            await pipe.CreateClientAsync(pipeName, "送る文字列１");
            await Task.Delay(2000);
            await pipe.CreateClientAsync(pipeName, "送る文字列２");
            await Task.Delay(2000);

            // キャンセル(サーバーを終了させる)
            ConsoleWriteLine($"  Main：パイプサーバーをキャンセルします");
            _cancelServer.Cancel();

            await Task.Delay(1000);

            // キャンセルした後の送信ができないことを見る
            try
            {
                await pipe.CreateClientAsync(pipeName, "送る文字列３");
            }
            catch(Exception ex)
            {
                ConsoleWriteLine($"送信失敗 : {ex.GetType()} {ex.Message}");
            }

            ConsoleWriteLine($"-----実験終了-----");
            Console.ReadLine();
        }

        private static void ConsoleWriteLine(string log)
        {
            Debug.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} {log}");
        }
    }
}