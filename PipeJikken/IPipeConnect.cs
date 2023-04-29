namespace PipeJikken
{
    public interface IPipeConnect
    {
        Task CreateServerAsync(string pipeName, Action<string> onRecv, CancellationToken ct = default);
        Task CreateClientAsync(string pipeName, string writeString);
    }
}
