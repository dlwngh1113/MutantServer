using mutant_server;
using System;
using System.Numerics;

namespace StressClient
{
    class Client
    {
        public AsyncUserToken asyncUserToken;
        public string name;
        public MyVector3 position;
        public MyVector3 rotation;
        public MyVector3 posVelocity;
        public MyVector3 rotVelocity;

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
                    this.posVelocity.x = 10f;
                    break;
                case 1:
                    this.posVelocity.x = 10f;
                    break;
                case 2:
                    this.posVelocity.z = 10f;
                    break;
                case 3:
                    this.posVelocity.z = -10f;
                    break;
            }

            PlayerStatusPacket packet = new PlayerStatusPacket(this.asyncUserToken.writeEventArgs.Buffer, 0);
            packet.id = this.id;
            packet.name = this.name;
            packet.time = MutantGlobal.GetCurrentMilliseconds();

            packet.position = this.position;
            packet.rotation = this.rotation;
            packet.posVelocity = this.posVelocity;
            packet.rotVelocity = this.rotVelocity;
            
            packet.PacketToByteArray(MutantGlobal.CTOS_STATUS_CHANGE);
            this.asyncUserToken.socket.SendAsync(this.asyncUserToken.writeEventArgs);
        }
    }
}
