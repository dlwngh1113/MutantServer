using System.Net.Sockets;

namespace mutant_server
{
    public class Client
    {
        public AsyncUserToken asyncUserToken;
        public int userID;
        public string userName = null;
        public Client(int id)
        {
            this.userID = id;
        }
    }
}