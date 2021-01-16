using System.Net.Sockets;

namespace mutant_server
{
    class Client
    {
        public SocketAsyncEventArgs socketAsyncEventArgs;
        public int userID;
        public string userName;
        public Client(int id)
        {
            this.userID = id;
        }
    }
}