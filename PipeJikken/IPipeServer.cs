namespace PipeJikken;

public interface IPipeServer
{
    void Create(string pipeName);
    Task StartAsync(Action<string> onRecv, CancellationToken ct = default);
}
