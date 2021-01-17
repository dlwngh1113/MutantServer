using mutant_server;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace mutant_client
{
    class Player
    {
        Socket _socket;
        SocketAsyncEventArgs _readEventArgs;
        SocketAsyncEventArgs _writeEventArgs;
        byte[] _readBuffer;
        byte[] _writeBuffer;
        string _name;
        public Player(Socket s)
        {
            _socket = s;
            Init();
        }

        private void Init()
        {
            _readEventArgs = new SocketAsyncEventArgs();
            _writeEventArgs = new SocketAsyncEventArgs();
            _readEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
            _writeEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
            _readEventArgs.UserToken = new AsyncUserToken();
            _writeEventArgs.UserToken = new AsyncUserToken();
            ((AsyncUserToken)_readEventArgs.UserToken).socket = _socket;
            ((AsyncUserToken)_writeEventArgs.UserToken).socket = _socket;

            _writeBuffer = new byte[MutantGlobal.BUF_SIZE];
            _readBuffer = new byte[MutantGlobal.BUF_SIZE];

            Console.Write("플레이어 이름을 입력해주세요:");
            string name = Console.ReadLine();
            _name = name;

            Login_Request();
        }
        private void Login_Request()
        {
            ((AsyncUserToken)_writeEventArgs.UserToken).operation = MutantGlobal.CTOS_LOGIN;
            Packet p = new Packet();
            p.name = _name;
            _writeBuffer = MutantGlobal.ObjectToByteArray(p);

            _socket.SendAsync(_writeEventArgs);
        }
        private void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                    ProcessSend(e);
                    break;
                default:
                    throw new ArgumentException("The last operation completed on the socket was not a receive or send");
            }
        }
        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            AsyncUserToken token = (AsyncUserToken)e.UserToken;
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                switch(token.operation)
                {
                    case MutantGlobal.STOC_LOGIN_OK:
                        break;
                    case MutantGlobal.STOC_LOGIN_FAIL:
                        break;
                    case MutantGlobal.STOC_STATE_CHANGE:
                        break;
                    case MutantGlobal.STOC_ENTER:
                        break;
                    case MutantGlobal.STOC_LEAVE:
                        break;
                    case MutantGlobal.STOC_CHAT:
                        break;
                    default:
                        throw new Exception("operation from server is not valid\n");
                }
            }
        }
        private void ProcessSend(SocketAsyncEventArgs e)
        {
        }
        public void MySend()
        {
            _writeBuffer = System.Text.Encoding.UTF8.GetBytes("test sending\n");
            _writeEventArgs.SetBuffer(_writeBuffer, 0, _writeBuffer.Length);
            _socket.SendAsync(_writeEventArgs);

        }
        public void SendChattingPacket()
        {
            Console.Write("보낼 메세지를 입력해주세요:");
            string msg = Console.ReadLine();
            _writeBuffer = System.Text.Encoding.UTF8.GetBytes(msg);
            _writeEventArgs.SetBuffer(_writeBuffer, 0, _writeBuffer.Length);
            ((AsyncUserToken)_writeEventArgs.UserToken).operation = MutantGlobal.CTOS_CHAT;

            _socket.SendAsync(_writeEventArgs);
        }
    }
    class Client
    {
        private List<Player> _lisg;
        public Client()
        {
            _lisg = new List<Player>();
        }
        public void Run()
        {
            Console.Write("서버의 IP주소를 입력해주세요:");
            string serverIp = Console.ReadLine();
            Player p;
            Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            clientSocket.Connect(serverIp, MutantGlobal.PORT);
            p = new Player(clientSocket);
            while (true)
            {
                try
                {
                    int key = Console.Read();
                    if (key == 'c')
                    {
                        p.SendChattingPacket();
                    }
                }
                catch (Exception ex)
                {
                    string s = ex.Message;
                    Console.WriteLine(s);
                    break;
                }
            }
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            Client cl = new Client();
            cl.Run();
        }
    }
}