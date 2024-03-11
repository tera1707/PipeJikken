using System.Diagnostics;
using System.IO.Pipes;
using System.Security.Principal;
using System.Text;

namespace PipeJikken
{
    public class PipeConnect :IDisposable, IPipeConnect
    {
        private static readonly int RecvPipeThreadMax = 1;
        private CancellationTokenSource _lifetimeCts = new CancellationTokenSource();
        private bool disposedValue;

        // パイプから受信を行う処理
        // 送信(クライアント)側が切断(close)すると、IOExceptionが来るので再度パイプサーバー作成しなおしする。
        public Task CreateServerAsync(string pipeName, Action<string> onRecv, CancellationToken ct = default)
        {
            var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _lifetimeCts.Token);

            return Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        // 同じパイプに対しての接続は1件まで
                        using (var pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.InOut, RecvPipeThreadMax, PipeTransmissionMode.Byte, PipeOptions.CurrentUserOnly))
                        {
                            // クライアントからの接続を待つ
                            Debug.WriteLine("Waiting for connection...");
                            pipeServer.WaitForConnection();

                            // クライアントからのメッセージを受け取る
                            byte[] buffer = new byte[256];
                            int bytesRead = pipeServer.Read(buffer, 0, buffer.Length);
                            string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                            Debug.WriteLine("Server Received: {0}", message);

                            onRecv?.Invoke(message);

                            // クライアントに応答を送信
                            var response = "Server response string.";
                            byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                            pipeServer.Write(responseBytes, 0, responseBytes.Length);
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
                        ConsoleWriteLine($"受信：パイプサーバーのキャンセル要求がきました。{oce.GetType()}");
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
        public async Task CreateClientAsync(string pipeName, string writeString, Action<string>? onRecvResponse = default)
        {
            await Task.Run(async () =>
            {
                try
                {
                    using (var pipeClient = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.CurrentUserOnly, TokenImpersonationLevel.Impersonation))
                    {
                        // クライアントからの接続を待つ
                        Debug.WriteLine("Waiting for connection...");
                        await pipeClient.ConnectAsync(1000);

                        // クライアントに応答を送信
                        var response = "Client Message string.";
                        byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                        pipeClient.Write(responseBytes, 0, responseBytes.Length);

                        // クライアントからのメッセージを受け取る
                        var buffer = new byte[256];
                        int bytesRead = pipeClient.Read(buffer, 0, buffer.Length);
                        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        Debug.WriteLine("Client Received: {0}", message);

                        onRecvResponse?.Invoke(message);
                    }
                }
                catch (TimeoutException te)
                {
                    ConsoleWriteLine(te.Message);
                }

                ConsoleWriteLine(" 送信：パイプ終了");
            });
        }

        private static void ConsoleWriteLine(string log)
        {
            Debug.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} {log}");
        }

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: マネージド状態を破棄します (マネージド オブジェクト)
                    _lifetimeCts.Cancel();
                }

                // TODO: アンマネージド リソース (アンマネージド オブジェクト) を解放し、ファイナライザーをオーバーライドします
                // TODO: 大きなフィールドを null に設定します
                disposedValue = true;
            }
        }

        // // TODO: 'Dispose(bool disposing)' にアンマネージド リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします
        // ~PipeConnect()
        // {
        //     // このコードを変更しないでください。クリーンアップ コードを 'Dispose(bool disposing)' メソッドに記述します
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // このコードを変更しないでください。クリーンアップ コードを 'Dispose(bool disposing)' メソッドに記述します
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}