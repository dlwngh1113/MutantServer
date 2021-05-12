using System.Net;

namespace mutant_server
{
    public delegate void UpdateMethod(float elapsedTime);
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