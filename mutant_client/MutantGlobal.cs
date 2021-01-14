using System.Numerics;

namespace mutant_server
{
    public class MutantGlobal
    {
        public static short BUF_SIZE = 1024;
        public static short MAX_USERS = 10000;
        public static short PORT = 9000;
    }
    public class Packet
    {
        public short size;
        public short type;
    }
    public class Transform : Packet
    {
        public Vector3 position;
        public Vector3 rotation;
        public Vector3 scale;
    }
}