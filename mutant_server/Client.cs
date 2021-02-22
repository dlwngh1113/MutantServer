using System.Net.Sockets;

namespace mutant_server
{
    class Client
    {
        public AsyncUserToken asyncUserToken;
        public int userID;
        public string userName = null;
        public MyVector3 position;
        public MyVector3 rotation;
        public MyVector3 posVelocity;
        public MyVector3 rotVelocity;
        public Client(int id)
        {
            this.userID = id;
        }
    }
}