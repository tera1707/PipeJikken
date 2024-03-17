using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PipeJikken
{
    public interface IPipeClient
    {
        Task Create(string pipeName);
        Task SendAsync(string writeString, Action<string>? onRecvResponse = default);
    }
}
