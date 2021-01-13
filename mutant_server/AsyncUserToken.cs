using System.Net.Sockets;

namespace mutant_server
{
    class AsyncUserToken
    {
        public Socket socket;
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
