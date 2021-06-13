﻿using System;

namespace mutant_server
{
    //client to server operation
    enum CTOS_OP
    {
        //lobby, main server operation
        CTOS_LOGIN,
        CTOS_LOGOUT,
        CTOS_JOIN_GAME,
        CTOS_CREATE_ROOM,
        CTOS_SELECT_ROOM,
        CTOS_REFRESH_ROOMS,

        //ingame operation
        CTOS_STATUS_CHANGE = 100,
        CTOS_ATTACK,
        CTOS_CHAT,
        CTOS_ITEM_CLICKED,
        CTOS_ITEM_CRAFT_REQUEST,
        CTOS_LEAVE_GAME,
        CTOS_VOTE_REQUEST,
        CTOS_VOTE_SELECTED,
    }

    enum STOC_OP
    {
        STOC_LOGIN_OK,
        STOC_LOGIN_FAIL,
        STOC_ENTER_FAIL,
        STOC_ENTER_OK,
        STOC_ROOM_CREATE_FAIL,
        STOC_ROOM_CREATE_SUCCESS,

        STOC_STATUS_CHANGE = 100,
        STOC_PLAYER_ENTER,
        STOC_PLAYER_LEAVE,
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
        public const int FrameRate = (int)((1.0 / 60.0) * 1000);

        public static int id = 0;

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
                //Swap<byte>(ref ary[random.Next(0, ary.Length)], ref ary[random.Next(0, ary.Length)]);
            }

            return ary;
        }
    }
}