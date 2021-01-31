using mutant_server;
using System;
using System.Numerics;

namespace StressClient
{
    class Client
    {
        public AsyncUserToken asyncUserToken;
        public string name;
        public Vector3 position;
        public int id;
        public Client(int id)
        {
            this.id = id;
            name = "test" + id;
        }
        public void RandomBehaviour()
        {
            Random random = new Random();
            switch(random.Next(6))
            {
                case 0:
                    break;
                case 1:
                    break;
                case 2:
                    break;
                case 3:
                    break;
                case 4:
                    break;
                case 5:
                    break;
                default:
                    throw new Exception("unknown behaviour in client");
            }
        }
    }
}
