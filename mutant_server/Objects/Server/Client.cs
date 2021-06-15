using System.Collections.Generic;

namespace mutant_server
{
    public class Client
    {
        public AsyncUserToken asyncUserToken;
        public int userID;
        public string userName = "admin";

        public string passWd = "";
        public int winCoundTrator = 0;
        public int winCountResearcher = 0;
        public int winCountNocturn = 0;
        public int winCountPsychy = 0;
        public int winCountTanker = 0;

        public MyVector3 InitPos;
        public MyVector3 position;
        public MyVector3 rotation;
        public Dictionary<string, int> inventory;
        public byte job;
        public Client(int id)
        {
            this.userID = id;
            inventory = new Dictionary<string, int>();
        }
    }
}