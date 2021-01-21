using System.Net.Sockets;

namespace mutant_server
{
    public class AsyncUserToken
    {
        public Socket socket = null;
        public SocketAsyncEventArgs readEventArgs = null;
        public SocketAsyncEventArgs writeEventArgs = null;
        public byte operation = 0;
        public AsyncUserToken()
        {

        }
        public AsyncUserToken(Socket s)
        {
            this.socket = s;
        }
    }
}
