using System.Net;

namespace mutant_server
{
    class Program
    {
        static public void Main(string[] args)
        {
            Server server = new Server(MutantGlobal.MAX_USERS, MutantGlobal.BUF_SIZE);
            server.Init();
            server.Start(new IPEndPoint(IPAddress.Any, MutantGlobal.PORT));
        }
    }
}