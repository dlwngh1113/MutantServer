using System.Collections.Generic;

namespace mutant_server
{
    class Client
    {
        public AsyncUserToken asyncUserToken;
        public int userID;
        public string userName = null;
        public MyVector3 position;
        public MyVector3 rotation;
        public Dictionary<string, int> inventory;
        public Client(int id)
        {
            this.userID = id;
            inventory = new Dictionary<string, int>();
        }
    }
}