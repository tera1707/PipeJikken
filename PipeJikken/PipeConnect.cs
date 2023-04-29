using System.Diagnostics;
using System.IO.Pipes;
using System.Security.Principal;

namespace PipeJikken
{    public class PipeConnect
    {
        private static readonly int recvPipeThreadMax = 1;

        // パイプから受信を行う処理
        // 送信(クライアント)側が切断(close)すると、IOExceptionが来るので再度パイプサーバー作成しなおしする。
        public Task CreateServerAsync(string pipeName, Action<string> onRecv, CancellationToken ct = default)
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

                                onRecv.Invoke(recvString ?? "null");
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
        public async Task CreateClientAsync(string pipeName, string writeString)
        {
            await Task.Run(async () =>
            {
                using (var pipeClient = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly, TokenImpersonationLevel.Impersonation))
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
            Debug.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} {log}");
        }
    }
}