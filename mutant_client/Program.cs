using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Windows.Forms;
using mutant_server;

namespace mutant_client
{
    class Player
    {
        Socket _socket;
        SocketAsyncEventArgs _readEventArgs;
        SocketAsyncEventArgs _writeEventArgs;
        byte[] _readBuffer;
        byte[] _writeBuffer;
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
            _readEventArgs.UserToken = _socket;
            _writeEventArgs.UserToken = _socket;

            _writeBuffer = new byte[MutantGlobal.BUF_SIZE];
            _readBuffer = new byte[MutantGlobal.BUF_SIZE];
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
            _socket.ReceiveAsync(_readEventArgs);
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
                    if (key == 'w')
                    {
                        p.MySend();
                    }
                    //_lisg.Add(new Player(clientSocket));
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