using System.IO.Pipes;
using System.Text;
using BomBom.Misc;

namespace BomBom.IPC;

public class Server
{
    public async Task ReadySend(string name, string data)
    {
        BomBomLogger.Log(BomBomLogger.LogType.INFO, "IPC-SERVER", $"Opening {name}");
        await using NamedPipeServerStream pipeServer = new NamedPipeServerStream(name, PipeDirection.Out);
        await pipeServer.WaitForConnectionAsync();

        byte[] buffer = Encoding.UTF8.GetBytes(data);
        await pipeServer.WriteAsync(buffer);

        BomBomLogger.Log(BomBomLogger.LogType.INFO, "IPC-SERVER", $"Closing {name}");
        pipeServer.Close();
    }
}
