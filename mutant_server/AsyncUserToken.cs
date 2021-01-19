using System.Net.Sockets;

namespace mutant_server
{
    public class AsyncUserToken
    {
        public Socket socket;
        public SocketAsyncEventArgs readEventArgs { get; private set; }
        public SocketAsyncEventArgs writeEventArgs { get; private set; }
        public byte operation;
        public AsyncUserToken(Socket s)
        {
            this.socket = s;
        }
        public void SetWriteEventArgs(SocketAsyncEventArgs e)
        {
            this.writeEventArgs = e;
        }
        public void SetReadEventArgs(SocketAsyncEventArgs e)
        {
            this.readEventArgs = e;
        }
        public AsyncUserToken()
        {
            socket = null;
        }
    }
}
