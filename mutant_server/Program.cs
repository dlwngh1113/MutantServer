using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace mutant_server
{
    public delegate void UpdateMethod(object elapsedTime);
    public delegate void CloseMethod(SocketAsyncEventArgs e);
    class Program
    {
        static public void Main(string[] args)
        {
            Server server = new Server(Defines.MAX_USERS, Defines.BUF_SIZE);
            server.Init();
            server.Start(new IPEndPoint(IPAddress.Any, Defines.PORT));
        }
    }
}