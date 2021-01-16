using System.Numerics;

namespace mutant_server
{
    public class MutantGlobal
    {
        public static short BUF_SIZE = 1024;
        public static short MAX_USERS = 10000;
        public static short PORT = 9000;

        public static int id = 0;

        public const byte CTOS_STATE_CHANGE = 0;

        public const byte STOC_STATE_CHANGE = 0;
    }
    public class Packet
    {
        public short type;
        public short size;
        public short id;
    }
    public class MovePacket : Packet
    {
        public Vector3 position;
        public Vector3 rotation;
        public Vector3 scale;
    }
}