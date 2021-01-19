using System.Numerics;

namespace mutant_server
{
    public class MutantPacket
    {
        public string name;
        public int id;
    }
    public class MovePacket : MutantPacket
    {
        public Vector3 position;
        public Vector3 rotation;
        public Vector3 scale;
    }
}