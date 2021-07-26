using System;

namespace mutant_server
{
    //client to server operation
    public enum CTOS_OP
    {
        //lobby, main server operation
        CTOS_LOGIN,
        CTOS_LOGOUT,
        CTOS_LEAVE_ROOM,
        CTOS_CREATE_ROOM,
        CTOS_SELECT_ROOM,
        CTOS_GET_ROOM_USERS,
        CTOS_REFRESH_ROOMS,
        CTOS_CREATE_USER_INFO,
        CTOS_READY,
        CTOS_UNREADY,
        CTOS_GAME_START,
        CTOS_GET_HISTORY,

        //ingame operation
        CTOS_STATUS_CHANGE = 100,
        CTOS_LEAVE_GAME,
        CTOS_ATTACK,
        CTOS_CHAT,
        CTOS_ITEM_CLICKED,
        CTOS_ITEM_CRAFT_REQUEST,
        CTOS_VOTE_REQUEST,
        CTOS_VOTE_SELECTED,
    }

    //server to client operation
    public enum STOC_OP
    { 
        //lobby, main server operation
        STOC_LOGIN_OK,
        STOC_LOGIN_FAIL,
        STOC_ROOM_ENTER_SUCCESS,
        STOC_PLAYER_ENTER,
        STOC_PLAYER_LEAVE_ROOM,
        STOC_ROOM_ENTER_FAIL,
        STOC_ROOM_CREATE_SUCCESS,
        STOC_ROOM_REFRESHED,
        STOC_CREATE_USER_INFO_SUCCESS,
        STOC_CREATE_USER_INFO_FAIL,
        STOC_READY,
        STOC_UNREADY,
        STOC_GAME_START,
        STOC_PROVISION_HISTORY,

        //ingame operation
        STOC_STATUS_CHANGE = 100,
        STOC_PLAYER_LEAVE_GAME,
        STOC_CHAT,
        STOC_ITEM_GAIN,
        STOC_ITEM_DENIED,
        STOC_ITEM_CRAFTED,
        STOC_SYSTEM_CHANGE,
        STOC_KILLED,
        STOC_VOTE_START,
        STOC_VOTED,
    }
    public class Defines
    {
        public const short BUF_SIZE = 1024;
        public const short MAX_USERS = 10000;
        public const short MAX_CHAT_LEN = 100;
        public const short PORT = 9000;
        public const float FrameRate = (float)((1.0 / 60.0) * 1000);
        public const byte MAX_ROOM_USER = 5;

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
        public const byte ITEM_AXE = 0;
        public const byte ITEM_LOG = 1;
        public const byte ITEM_STICK = 2;
        public const byte ITEM_ROCK = 3;
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

        public const byte ROOM_WAIT = 0;
        public const byte ROOM_PLAYING = 1;
        public const byte ROOM_RESULT = 2;

        public static void Swap<T> (ref T a, ref T b)
        {
            var tmp = a;
            a = b;
            b = tmp;
        }

        public static byte[] GenerateRandomJobs()
        {
            byte[] ary = { JOB_TRACKER, JOB_PSYCHY, JOB_NOCTURN, JOB_RESEARCHER, JOB_TANKER };
            //Random random = new Random();
            //for(int i=0;i<ary.Length;++i)
            //{
            //    Swap<byte>(ref ary[random.Next(0, ary.Length)], ref ary[random.Next(0, ary.Length)]);
            //}

            return ary;
        }
    }
}