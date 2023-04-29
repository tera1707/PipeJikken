using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PipeJikken
{
    public interface IPipeConnect
    {
        Task CreateServerAsync(string pipeName, Action<string> onRecv, CancellationToken ct = default);
        Task CreateClientAsync(string pipeName, string writeString);
    }
}
