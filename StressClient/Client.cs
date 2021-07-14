using mutant_server;
using mutant_server.Packets;
using System;

namespace StressClient
{
    class Client
    {
        public AsyncUserToken asyncUserToken;
        public string name;
        public MyVector3 position;
        public MyVector3 rotation;

        public int id;
        public Client(int id)
        {
            this.id = id;
            name = "test" + id;
        }

        public void RandomBehaviour()
        {
            Random random = new Random();
            switch(5)
            {
                case 0:
                    //break;
                case 1:
                    //break;
                case 2:
                    //break;
                case 3:
                    //break;
                case 4:
                    //break;
                case 5:
                    RandomMove();
                break;
                default:
                    throw new Exception("unknown behaviour in client");
            }
        }

        private void RandomMove()
        {
            Random random = new Random();
            switch(random.Next(4))
            {
                case 0:
                    break;
                case 1:
                    break;
                case 2:
                    break;
                case 3:
                    break;
            }

            PlayerStatusPacket packet = new PlayerStatusPacket(this.asyncUserToken.writeEventArgs.Buffer, 0);
            packet.id = this.id;
            packet.name = this.name;
            packet.time = 0;

            packet.position = this.position;
            packet.rotation = this.rotation;
            
            packet.PacketToByteArray((byte)CTOS_OP.CTOS_STATUS_CHANGE);

            asyncUserToken.SendData(packet);
        }
    }
}
