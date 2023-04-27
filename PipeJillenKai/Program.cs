using System.IO.Pipes;
using System.Security.Principal;

namespace PipeJikkenKai
{
    internal class Program
    {
        private static int recvPipeThreadMax = 1;
        private static CancellationTokenSource? _cancelServer;
        private static CancellationTokenSource? _cancelClient;

        static async Task Main(string[] args)
        {
            _cancelServer = new CancellationTokenSource();
            _cancelClient = new CancellationTokenSource();

            ConsoleWriteLine("-----実験開始-----");

            var pipeName = "MyPipe1";

            // 受信Pipeサーバー立ち上げ
            _ = CreatePipeServerAsync(pipeName, _cancelServer.Token);

            // 送信
            await CreateClientAsync(pipeName, "送る文字列１");
            await Task.Delay(2000);
            await CreateClientAsync(pipeName, "送る文字列２");
            await Task.Delay(2000);

            // キャンセル(サーバーを終了させる)
            ConsoleWriteLine($"  Main：パイプサーバーをキャンセルします");
            _cancelServer.Cancel();

            //await Task.Delay(1000);

            // キャンセルした後の送信ができないことを見る
            try
            {
                await CreateClientAsync(pipeName, "送る文字列３");
            }
            catch(Exception ex)
            {
                ConsoleWriteLine($"送信失敗 : {ex.Message}");
            }

            ConsoleWriteLine($"-----実験終了-----");
            Console.ReadLine();
        }

        // パイプから受信を行う処理
        // 送信(クライアント)側が切断(close)すると、IOExceptionが来るので再度パイプサーバー作成しなおしする。
        public static Task CreatePipeServerAsync(string pipeName, CancellationToken ct = default)
        {
            return Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        // 同じパイプに対しての接続は1件まで
                        using (var pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.InOut, recvPipeThreadMax, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
                        {
                            // クライアントの接続待ち
                            ConsoleWriteLine($"受信：クライアントの接続待ち開始");
                            await pipeServer.WaitForConnectionAsync(ct);

                            ConsoleWriteLine($"受信：StreamReader");
                            using (var reader = new StreamReader(pipeServer))
                            {
                                // 受信待ち
                                ConsoleWriteLine($"受信：読み込み開始");
                                var recvString = await reader.ReadLineAsync();

                                ConsoleWriteLine($"受信：受信文字列：{recvString ?? "null"}");
                            }
                        }
                    }
                    catch (IOException ofex)
                    {
                        // クライアントが切断
                        ConsoleWriteLine("受信：クライアント側が切断しました");
                        ConsoleWriteLine(ofex.Message);
                    }
                    catch (OperationCanceledException oce)
                    {
                        // パイプサーバーのキャンセル要求(OperationCanceledExceptionをthrowしてTaskが終わると、Taskは「Cancel」扱いになる)
                        ConsoleWriteLine("受信：パイプサーバーのキャンセル要求がきました");
                        throw;
                    }
                    finally
                    {
                        ConsoleWriteLine("受信：パイプ終了");
                    }
                }
            });
        }

        // パイプに対して送信を行う処理
        // 1件送信するごとに、パイプ接続→切断するタイプ。
        public static async Task CreateClientAsync(string pipeName, string writeString)
        {
            await Task.Run(async () =>
            {
                using (var pipeClient = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous, TokenImpersonationLevel.Impersonation))
                {
                    await pipeClient.ConnectAsync(1000);

                    using (var writer = new StreamWriter(pipeClient))
                    {
                        await writer.WriteLineAsync(writeString);
                        writer.Flush();

                        ConsoleWriteLine(" 送信完了");
                    }
                }

                ConsoleWriteLine(" 送信：パイプ終了");
            });
        }

        private static void ConsoleWriteLine(string log)
        {
            Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} {log}");
        }
    }
}