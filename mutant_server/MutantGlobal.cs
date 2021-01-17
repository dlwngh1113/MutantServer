using System.ComponentModel;
using System.IO;
using System.Numerics;
using System.Runtime.Serialization.Formatters.Binary;

namespace mutant_server
{
    public class MutantGlobal
    {
        public static short BUF_SIZE = 1024;
        public static short MAX_USERS = 10000;
        public static short PORT = 9000;

        public static int id = 0;

        public const byte CTOS_LOGIN = 0;
        public const byte CTOS_STATE_CHANGE = 1;
        public const byte CTOS_ATTACK = 2;
        public const byte CTOS_CHAT = 3;
        public const byte CTOS_LOGOUT = 4;

        public const byte STOC_LOGIN_OK = 0;
        public const byte STOC_STATE_CHANGE = 1;
        public const byte STOC_ENTER = 2;
        public const byte STOC_LEAVE = 3;
        public const byte STOC_CHAT = 4;
        public const byte STOC_LOGIN_FAIL = 5;
        static public byte[] ObjectToByteArray(object obj)
        {
            //if (obj == null)
            //    return null;

            //BinaryFormatter bf = new BinaryFormatter();
            //MemoryStream ms = new MemoryStream();
            //bf.Serialize(ms, obj);
            //return ms.ToArray();
            TypeConverter temp = TypeDescriptor.GetConverter(obj.GetType());
            byte[] bt = (byte[])temp.ConvertTo(obj, typeof(byte[]));
            return bt;
        }
        static public object ByteArrayToObject(byte[] byteArr)
        {
            if (byteArr == null)
                return null;

            MemoryStream ms = new MemoryStream();
            BinaryFormatter bf = new BinaryFormatter();
            ms.Write(byteArr, 0, byteArr.Length);
            ms.Seek(0, SeekOrigin.Begin);
            object obj = (object)bf.Deserialize(ms);

            return obj;
        }
    }
    public class Packet
    {
        public string name;
        public short id;
    }
    public class MovePacket : Packet
    {
        public Vector3 position;
        public Vector3 rotation;
        public Vector3 scale;
    }
}