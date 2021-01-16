using System.Net.Sockets;

namespace mutant_server
{
    public class AsyncUserToken
    {
        public Socket socket;
        public byte operation;
        public AsyncUserToken(Socket s)
        {
            this.socket = s;
        }
        public AsyncUserToken()
        {
            socket = null;
        }
    }
}
