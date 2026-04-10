using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Reflection.PortableExecutable;
using System.Text;

namespace PipeJikken;

public class PipeServer : IDisposable, IPipeServer
{
    private static readonly int RecvPipeThreadMax = 1;
    private static readonly TimeSpan SendTimeout = TimeSpan.FromSeconds(1);
    private NamedPipeServerStream? pipeServer;

    private StreamWriter? streamWriter;
    private StreamReader? streamReader;

    private CancellationTokenSource _lifetimeCts = new CancellationTokenSource();
    private bool disposedValue;

    public void Create(string pipeName)
    {
        pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.InOut, RecvPipeThreadMax, PipeTransmissionMode.Byte, PipeOptions.CurrentUserOnly | PipeOptions.Asynchronous);

        streamWriter = new StreamWriter(pipeServer, Encoding.UTF8);
        streamReader = new StreamReader(pipeServer, Encoding.UTF8);
    }

    // パイプサーバーを起動する処理
    public Task StartAsync(Action<string> onRecv, CancellationToken ct = default)
    {
        if (pipeServer is not null && pipeServer.IsConnected)
        {
            ConsoleWriteLine("クライアントがすでに接続中です。");
            return Task.CompletedTask;
        }

        var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _lifetimeCts.Token);

        return Task.Run(async () =>
        {
            // 接続待ちループ
            while (true)
            {
                try
                {
                    ConsoleWriteLine("接続待ち開始");

                    // クライアントからの接続を待つ
                    // サーバー破棄(Dispose)時に、キャンセルされて接続待ちループを抜ける
                    await pipeServer!.WaitForConnectionAsync(combinedCts.Token);
                    ConsoleWriteLine("クライアントに接続されました。");

                    // 受信待ちループ
                    while (true)
                    {
                        try
                        {
                            // クライアントからのメッセージを受け取る
                            ConsoleWriteLine("受信待ち開始");
                            var message = await RecvString();

                            if (message == null)
                            {
                                // nullの場合は、接続がクライアントから切られた
                                ConsoleWriteLine("クライアントから切断されました。");

                                if (pipeServer is null)
                                {
                                    ConsoleWriteLine("pipeServerがnull：受信待ち中にDisposeされた場合");
                                    throw new OperationCanceledException("pipeServerがnull：受信待ち中にDisposeされた場合");
                                }

                                if (pipeServer.IsConnected)
                                {
                                    pipeServer.Disconnect();
                                    ConsoleWriteLine("クライアントから切断されました。再接続待ちに戻ります。");
                                    break;
                                }
                            }

                            // 受信時処理を実行
                            onRecv?.Invoke(message);
                        }
                        finally
                        {
                            ConsoleWriteLine("受信完了");
                        }
                    }

                }
                catch (Exception ex)
                {
                    // デバッグ用trycatch
                    ConsoleWriteLine($"例外発生: {ex.Message}");
                    throw;
                }
            }
        }).ContinueWith( (a) =>
        {
            ConsoleWriteLine("接続待ち終了");
        });
    }

    // 受信処理
    public async Task<string?> RecvString()
    {
        if (streamReader is null)
            throw new InvalidOperationException();

        return await Task.Run(async () =>
        {
            var recvString = await streamReader.ReadLineAsync();
            Debug.WriteLine($"受信：{recvString}");

            return recvString;
        });
    }

    // 送信処理（主にクライアント受信時の応答の送信を想定）
    public async Task SendString(string sendString)
    {
        if (streamWriter is null)
            throw new InvalidOperationException();

        CancellationTokenSource cts = new CancellationTokenSource();
        cts.CancelAfter(SendTimeout);

        // クライアントに応答を送信
        await streamWriter.WriteLineAsync(sendString.ToCharArray(), cts.Token);
        streamWriter.Flush();
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

            if (pipeServer is not null)
            {
                streamWriter?.Dispose();
                streamWriter = null;
                streamReader?.Dispose();
                streamReader = null;
                pipeServer.Dispose();
                pipeServer = null;
            }

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        // このコードを変更しないでください。クリーンアップ コードを 'Dispose(bool disposing)' メソッドに記述します
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    #endregion
}
