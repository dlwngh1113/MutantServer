using System.Net.Sockets;

namespace mutant_server
{
    public class AsyncUserToken
    {
        public Socket socket = null;
        public int userID;
        public SocketAsyncEventArgs readEventArgs = null;
        public SocketAsyncEventArgs writeEventArgs = null;
        private MessageResolver _messageResolver = new MessageResolver();
        public AsyncUserToken()
        {

        }
        public AsyncUserToken(Socket s)
        {
            this.socket = s;
        }
        public void ResolveMessage(byte[] ary, int offset, int bytesTransferred)
        {
            this._messageResolver.ResolveMessage(ary, offset, bytesTransferred);
        }
    }
}
