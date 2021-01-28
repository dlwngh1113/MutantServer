using mutant_server;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;

namespace mutant_client
{
    class Player
    {
        Socket _socket;
        SocketAsyncEventArgs _readEventArgs;
        SocketAsyncEventArgs _writeEventArgs;
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
            _readEventArgs.SetBuffer(new byte[MutantGlobal.BUF_SIZE], 0, MutantGlobal.BUF_SIZE);
            _writeEventArgs.SetBuffer(new byte[MutantGlobal.BUF_SIZE], 0, MutantGlobal.BUF_SIZE);
            _readEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(RecvCompleted);
            _writeEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(SendCompleted);

            AsyncUserToken token = new AsyncUserToken();
            token.socket = _socket;
            token.writeEventArgs = _writeEventArgs;
            token.readEventArgs = _readEventArgs;

            _readEventArgs.UserToken = token;
            _writeEventArgs.UserToken = token;

            Console.Write("플레이어 이름을 입력해주세요:");
            string name = Console.ReadLine();
            _name = name;

            Login_Request();
        }
        private void Login_Request()
        {
            MutantPacket p = new MutantPacket(_writeEventArgs.Buffer, 0);
            p.name = _name;
            p.id = 0;
            p.PacketToByteArray(MutantGlobal.CTOS_LOGIN);

            _socket.SendAsync(_writeEventArgs);

            bool willRaise = _socket.ReceiveAsync(_readEventArgs);
            if (!willRaise)
            {
                ProcessReceive(_readEventArgs);
            }
        }
        private void SendCompleted(object sender, SocketAsyncEventArgs e)
        {
            ProcessSend(e);
        }
        private void ProcessSend(SocketAsyncEventArgs e)
        {

        }
        private void RecvCompleted(object sender, SocketAsyncEventArgs e)
        {
            ProcessReceive(e);
        }
        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            AsyncUserToken token = (AsyncUserToken)e.UserToken;
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                switch(token.readEventArgs.Buffer[e.Offset])
                {
                    case MutantGlobal.STOC_LOGIN_OK:
                        //다음 scene 로드
                        break;
                    case MutantGlobal.STOC_LOGIN_FAIL:
                        //다른 조치를 취하도록 안내메시지 출력
                        break;
                    case MutantGlobal.STOC_STATE_CHANGE:
                        //플레이어의 상태 position, rotation, scale 변경
                        break;
                    case MutantGlobal.STOC_ENTER:
                        //주변의 클라이언트에게 새 클라이언트가 있다는 것을 알림
                        break;
                    case MutantGlobal.STOC_LEAVE:
                        //주변의 클라이언트에게 클라이언트가 시야에서 사라짐을 알림
                        break;
                    case MutantGlobal.STOC_CHAT:
                        //어떤 클라이언트가 메세지를 보냈는지 말함
                        ProcessChatting(e);
                        break;
                    default:
                        throw new Exception("operation from server is not valid\n");
                }
            }
        }
        private void ProcessChatting(SocketAsyncEventArgs e)
        {
            ChattingPakcet packet = new ChattingPakcet(e.Buffer, e.Offset);
            packet.ByteArrayToPacket();
            Console.WriteLine("{0}:{1}", packet.id, packet.message);

            bool willRaise = ((AsyncUserToken)e.UserToken).socket.ReceiveAsync(e);
            if (!willRaise)
            {
                ProcessReceive(e);
            }
        }
        public void MySend()
        {

        }
        public void SendChattingPacket()
        {
            Console.Write("보낼 메세지를 입력해주세요:");
            string msg = Console.ReadLine();
            ChattingPakcet packet = new ChattingPakcet(_writeEventArgs.Buffer, 0);
            packet.id = 0;
            packet.name = this._name;
            packet.message = msg;
            packet.PacketToByteArray(MutantGlobal.CTOS_CHAT);

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
            var time = DateTime.Now.Second;
            Player p;
            Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            clientSocket.Connect(serverIp, MutantGlobal.PORT);
            var duration = DateTime.Now.Second - time;
            Console.WriteLine("duration: {0} seconds", duration);
            p = new Player(clientSocket);
            while (true)
            {
                try
                {
                    p.SendChattingPacket();
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