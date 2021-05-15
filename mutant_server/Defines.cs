using System;

namespace mutant_server
{
    public class Defines
    {
        public const short BUF_SIZE = 1024;
        public const short MAX_USERS = 10000;
        public const short MAX_CHAT_LEN = 100;
        public const short PORT = 9000;

        public static int id = 0;

        /// <summary>
        /// client to server operation
        /// </summary>
        public const byte CTOS_LOGIN = 0;
        public const byte CTOS_STATUS_CHANGE = 1;
        public const byte CTOS_ATTACK = 2;
        public const byte CTOS_CHAT = 3;
        public const byte CTOS_LOGOUT = 4;
        public const byte CTOS_ITEM_CLICKED = 5;
        public const byte CTOS_LEAVE_GAME = 6;
        public const byte CTOS_JOIN_GAME = 7;
        public const byte CTOS_ITEM_CRAFT_REQUEST = 8;
        public const byte CTOS_VOTE_REQUEST = 9;
        public const byte CTOS_VOTE_SELECTED = 10;

        /// <summary>
        /// server to client operation
        /// </summary>
        public const byte STOC_LOGIN_OK = 0;
        public const byte STOC_STATUS_CHANGE = 1;
        public const byte STOC_PLAYER_ENTER = 2;
        public const byte STOC_PLAYER_LEAVE = 3;
        public const byte STOC_CHAT = 4;
        public const byte STOC_LOGIN_FAIL = 5;
        public const byte STOC_ITEM_GAIN = 6;
        public const byte STOC_ITEM_DENIED = 7;
        public const byte STOC_SYSTEM_CHANGE = 8;
        public const byte STOC_ENTER_FAIL = 9;
        public const byte STOC_KILLED = 10;
        public const byte STOC_ITEM_CRAFTED = 11;
        public const byte STOC_VOTE_START = 12;
        public const byte STOC_VOTED = 13;

        /// <summary>
        /// player motions
        /// </summary>
        public const byte PLAYER_IDLE = 0;
        public const byte PLAYER_WALKING = 1;
        public const byte PLAYER_RUNNING = 2;
        public const byte PLAYER_ATTACK = 3;
        public const byte PLAYER_HIT = 4;
        public const byte PLAYER_TALK = 5;

        /// <summary>
        /// items
        /// </summary>
        public const byte ITEM_LOG = 0;
        public const byte ITEM_STICK = 1;
        public const byte ITEM_ROCK = 2;
        public const byte ITEM_AXE = 3;
        public const byte ITEM_ROPE = 4;
        public const byte ITEM_PLANE = 5;
        public const byte ITEM_SAIL = 6;
        public const byte ITEM_PADDLE = 7;
        public const byte ITEM_BOAT = 8;

        public const byte JOB_TRACKER = 0;
        public const byte JOB_PSYCHY = 1;
        public const byte JOB_NOCTURN = 2;
        public const byte JOB_RESEARCHER = 3;
        public const byte JOB_TANKER = 4;

        public static int GetCurrentMilliseconds()
        {
            return (DateTime.Now.Second * 1000 + DateTime.Now.Millisecond);
        }

        public static void Swap<T> (ref T a, ref T b)
        {
            var tmp = a;
            a = b;
            b = tmp;
        }

        public static byte[] GenerateRandomJobs()
        {
            byte[] ary = { JOB_TRACKER, JOB_PSYCHY, JOB_NOCTURN, JOB_RESEARCHER, JOB_TANKER };
            Random random = new Random();
            for(int i=0;i<ary.Length;++i)
            {
                Swap<byte>(ref ary[random.Next(0, ary.Length)], ref ary[random.Next(0, ary.Length)]);
            }

            return ary;
        }
    }
}