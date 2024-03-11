namespace PipeJikken
{
    public interface IPipeConnect
    {
        Task StartServerAsync(Action<string> onRecv, CancellationToken ct = default);
        Task StartClientAsync(string writeString, Action<string>? onRecvResponse = default);
    }
}
