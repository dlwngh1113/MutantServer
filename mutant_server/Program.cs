using System.Net;
using System.Net.Sockets;

namespace mutant_server
{
    public delegate void UpdateMethod(object elapsedTime);
    public delegate void CloseMethod(SocketAsyncEventArgs e);
    public delegate void GetUserFromRoom(Client c);
    public delegate void GetUserEvent(SocketAsyncEventArgs e);

    class Program
    {
        static public void Main(string[] args)
        {
            Server server = new Server(10, Defines.BUF_SIZE);
            server.Init();
            server.Start(new IPEndPoint(IPAddress.Any, Defines.PORT));
        }
    }
}