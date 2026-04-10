namespace PipeJikken;

public interface IPipeClient
{
    Task Create(string pipeName);
    Task SendAsync(string writeString, Action<string>? onRecvResponse = default);
}
