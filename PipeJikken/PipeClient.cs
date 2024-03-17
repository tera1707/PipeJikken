using System.Diagnostics;
using System.IO.Pipes;
using System.Security.Principal;
using System.Text;

namespace PipeJikken
{

    public class PipeClient : IDisposable, IPipeClient
    {
        private NamedPipeClientStream? pipeClient;
        private CancellationTokenSource _lifetimeCts = new CancellationTokenSource();
        private bool disposedValue;

        public async Task Create(string pipeName)
        {
            if (pipeClient is not null && pipeClient.IsConnected)
                return;//すでに接続中

            pipeClient = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.CurrentUserOnly | PipeOptions.Asynchronous, TokenImpersonationLevel.Impersonation);

            // クライアントからの接続を待つ
            Debug.WriteLine("Waiting for connection...");

            try
            {
                await pipeClient.ConnectAsync(1000);
            }
            catch (TimeoutException toe)
            {
                ConsoleWriteLine(toe.Message);// 接続時にタイムアウトした場合はなにもしない
            }
        }

        // パイプに対して送信を行う処理
        // 1件送信するごとに、パイプ接続→切断するタイプ。
        public async Task SendAsync(string writeString, Action<string>? onRecvResponse = default)
        {
            if (pipeClient is null)
                throw new InvalidOperationException("PipeClientがnullです");

            await Task.Run(async () =>
            {
                try
                {
                    using var cts = new CancellationTokenSource();
                    cts.CancelAfter(TimeSpan.FromSeconds(5));

                    // クライアントに応答を送信
                    var response = "Client Message string.";
                    byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                    await pipeClient.WriteAsync(responseBytes, 0, responseBytes.Length, cts.Token);//PipeOptions.Asynchronous を設定しておかないと、cancelできない！

                    // クライアントからのメッセージを受け取る
                    var buffer = new byte[256];
                    int bytesRead = await pipeClient.ReadAsync(buffer, 0, buffer.Length, cts.Token);
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Debug.WriteLine($"Client Received: {0}", message);

                    onRecvResponse?.Invoke(message);
                }
                catch (TimeoutException te)
                {
                    ConsoleWriteLine(te.Message);
                }
                catch (OperationCanceledException oce)
                {
                    ConsoleWriteLine(oce.Message);
                }
                catch (IOException ioe)
                {
                    pipeClient?.Dispose();
                    pipeClient = null;
                    ConsoleWriteLine(ioe.Message);
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

                if (pipeClient is not null)
                {
                    pipeClient.Dispose();
                    pipeClient = null;
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
