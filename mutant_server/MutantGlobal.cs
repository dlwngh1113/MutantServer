namespace mutant_server
{
    public class MutantGlobal
    {
        public const short BUF_SIZE = 1024;
        public const short MAX_USERS = 10000;
        public const short MAX_CHAT_LEN = 100;
        public const short PORT = 9000;

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
    }
}