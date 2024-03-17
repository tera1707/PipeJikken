using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PipeJikken
{
    public interface IPipeServer
    {
        void Create(string pipeName);
        Task StartAsync(Action<string> onRecv, CancellationToken ct = default);
    }
}
