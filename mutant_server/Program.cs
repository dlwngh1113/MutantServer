using System.Net;
using System.Threading;

namespace mutant_server
{
    public delegate void UpdateMethod(object elapsedTime);
    class Program
    {
        static public void Main(string[] args)
        {
            Server server = new Server(Defines.MAX_USERS, Defines.BUF_SIZE);
            server.Init();
            server.Start(new IPEndPoint(IPAddress.Any, Defines.PORT));
            System.Threading.Timer timer = new System.Threading.Timer(sampleTest.Update, Defines.FrameRate, 0, Defines.FrameRate);
        }
    }
}