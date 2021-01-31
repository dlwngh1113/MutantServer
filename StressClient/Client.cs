using mutant_server;
using System.Numerics;

namespace StressClient
{
    class Client
    {
        public AsyncUserToken asyncUserToken;
        public string name;
        public Vector3 position;
        public int id
        {
            get => id;
            private set => id = value;
        }
        public Client(int id)
        {
            this.id = id;
        }
    }
}
