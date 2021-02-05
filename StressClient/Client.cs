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
        public Vector3 rotation;
        public Vector3 posVel;
        public Vector3 rotateVel;
        public int id;
        public Client(int id)
        {
            this.id = id;
            name = "test" + id;
            position.X = 0f;
            position.Y = 0f;
            position.Z = 0f;
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
                    this.posVel.X = 10f;
                    break;
                case 1:
                    this.posVel.X = -10f;
                    break;
                case 2:
                    this.posVel.Z = 10f;
                    break;
                case 3:
                    this.posVel.Z = -10f;
                    break;
            }

            PlayerStatusPacket packet = new PlayerStatusPacket(this.asyncUserToken.writeEventArgs.Buffer, 0);
            packet.id = this.id;
            packet.name = this.name;
            packet.time = MutantGlobal.GetCurrentMilliseconds();
            packet.posVel = this.posVel;
            packet.PacketToByteArray(MutantGlobal.CTOS_STATUS_CHANGE);
            this.asyncUserToken.socket.SendAsync(this.asyncUserToken.writeEventArgs);
        }
    }
}
