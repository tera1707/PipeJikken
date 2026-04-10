using System.Diagnostics;
using System.IO.Pipes;
using System.Reflection.PortableExecutable;
using System.Security.Principal;
using System.Text;

namespace PipeJikken;


public class PipeClient : IDisposable, IPipeClient
{
    private NamedPipeClientStream? pipeClient;

    private static readonly int ConnectTimeoutMilliseconds = 1000;
    private static readonly int SendTimeoutSeconds = 1;

    private StreamWriter? streamWriter;
    private StreamReader? streamReader;

    private CancellationTokenSource _lifetimeCts = new CancellationTokenSource();
    private bool disposedValue;

    private bool isSending = false;

    public async Task Create(string pipeName)
    {
        if (pipeClient is not null && pipeClient.IsConnected)
            return;//すでに接続中

        pipeClient = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.CurrentUserOnly | PipeOptions.Asynchronous, TokenImpersonationLevel.Impersonation);

        try
        {
            ConsoleWriteLine("接続開始");
            await pipeClient.ConnectAsync(ConnectTimeoutMilliseconds);
            ConsoleWriteLine("接続しました");

            streamWriter = new StreamWriter(pipeClient, Encoding.UTF8);
            streamReader = new StreamReader(pipeClient, Encoding.UTF8);
        }
        catch (TimeoutException toe)
        {
            ConsoleWriteLine("接続タイムアウト、何もせず終了します");
            ConsoleWriteLine(toe.Message);// 接続時にタイムアウトした場合はなにもしない
        }
    }

    // パイプに対して送信を行う処理
    public async Task SendAsync(string writeString, Action<string>? onRecvResponse = default)
    {
        if (pipeClient is null || streamWriter is null || streamReader is null)
            throw new InvalidOperationException("PipeClientがnullです");

        if (isSending)
            return;//送信中に送信要求来たらなにもしない

        isSending = true;

        await Task.Run(async () =>
        {
            try
            {
                using var cts = new CancellationTokenSource();
                cts.CancelAfter(TimeSpan.FromSeconds(SendTimeoutSeconds));

                await streamWriter.WriteLineAsync(writeString.ToCharArray(), cts.Token);
                streamWriter.Flush();

                ConsoleWriteLine("送信完了、応答待ち開始");

                var buffer = new char[1024];
                var bytesRead = await streamReader.ReadAsync(buffer, cts.Token); // →.NET7からは、ReadLineAsync()にcts.Tokenを受ける奴が追加されるのでそれ使えばよい
                var line = buffer.Take(5).ToArray()!;
                var payload = new string(line).TrimEnd('\r', '\n');

                ConsoleWriteLine($"応答受信完了：{payload}");

                onRecvResponse?.Invoke(payload);
            }
            catch (TimeoutException te)
            {
                ConsoleWriteLine(te.Message);
            }
            catch (OperationCanceledException oce)
            {
                streamWriter?.Dispose();
                streamWriter = null;

                streamReader?.Dispose();
                streamReader = null;

                pipeClient?.Dispose();
                pipeClient = null;

                ConsoleWriteLine(oce.Message);
            }
            catch (IOException ioe)
            {
                streamWriter?.Dispose();
                streamWriter = null;

                streamReader?.Dispose();
                streamReader = null;

                pipeClient?.Dispose();
                pipeClient = null;

                ConsoleWriteLine(ioe.Message);
            }

            ConsoleWriteLine(" 送信：パイプ終了");
        });

        isSending = false;
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

            streamWriter?.Dispose();
            streamWriter = null;

            streamReader?.Dispose();
            streamReader = null;

            pipeClient?.Dispose();
            pipeClient = null;

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
