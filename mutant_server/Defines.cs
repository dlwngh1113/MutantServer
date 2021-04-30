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

        /// <summary>
        /// player motions
        /// </summary>
        public const byte PLAYER_IDLE = 0;
        public const byte PLAYER_WALKING = 1;
        public const byte PLAYER_RUNNING = 2;
        public const byte PLAYER_ATTACK = 3;
        public const byte PLAYER_HIT = 4;
        public const byte PLAYER_TALK = 5;

        public const byte ITEM_LOG = 0;
        public const byte ITEM_STICK = 1;
        public const byte ITEM_ROCK = 2;
        public const byte ITEM_AXE = 3;
        public const byte ITEM_ROPE = 4;
        public const byte ITEM_PLANE = 5;
        public const byte ITEM_SAIL = 6;
        public const byte ITEM_PADDLE = 7;
        public const byte ITEM_BOAT = 8;

        public static int GetCurrentMilliseconds()
        {
            return (DateTime.Now.Second * 1000 + DateTime.Now.Millisecond);
        }
    }
}