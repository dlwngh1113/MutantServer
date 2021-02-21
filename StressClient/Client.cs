using mutant_server;
using System;
using System.Numerics;

namespace StressClient
{
    class Client
    {
        public AsyncUserToken asyncUserToken;
        public string name;
        public float xPosition = 0, yPosition = 0, zPosition = 0;
        public float xRotation = 0, yRotation = 0, zRotation = 0;
        public float xVelocity = 0, yVelocity = 0, zVelocity = 0;
        public float roll = 0, pitch = 0, yaw = 0;
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
                    this.xVelocity = 10f;
                    break;
                case 1:
                    this.xVelocity = -10f;
                    break;
                case 2:
                    this.zVelocity = 10f;
                    break;
                case 3:
                    this.zVelocity = -10f;
                    break;
            }

            PlayerStatusPacket packet = new PlayerStatusPacket(this.asyncUserToken.writeEventArgs.Buffer, 0);
            packet.id = this.id;
            packet.name = this.name;
            packet.time = MutantGlobal.GetCurrentMilliseconds();

            packet.xVelocity = this.xVelocity;
            packet.yVelocity = this.yVelocity;
            packet.zVelocity = this.zVelocity;
            
            packet.PacketToByteArray(MutantGlobal.CTOS_STATUS_CHANGE);
            this.asyncUserToken.socket.SendAsync(this.asyncUserToken.writeEventArgs);
        }
    }
}
