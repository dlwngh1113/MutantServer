using System.Net.Sockets;

namespace mutant_server
{
    public class AsyncUserToken
    {
        public Socket socket = null;
        public string name = null;
        public int userID;
        public SocketAsyncEventArgs readEventArgs = null;
        public SocketAsyncEventArgs writeEventArgs = null;
        public AsyncUserToken()
        {

        }
        public AsyncUserToken(Socket s)
        {
            this.socket = s;
        }
    }
}
