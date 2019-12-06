using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    //لیست نگهدارنده کانکشن های کلاینت ها
    static class ClsClients
    {
        public static Dictionary<int,ClsClient> Clients = new Dictionary<int, ClsClient>();
        public static int AddClient(Socket socket)
        {
            Clients.Add(Clients.Count, new ClsClient(socket, Clients.Count));
            return Clients.Count - 1;
        }
        public static void RemoveClient(int id) => Clients.Remove(id);
    }

    class ClsServerSocket
    {
        Socket _socket;
        const int _port = 1111;

        public ClsServerSocket()
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        public void StartServer()
        {
            try
            {
                Console.WriteLine($"start server at port : {_port}");
                _socket.Bind(new IPEndPoint(IPAddress.Any, _port));
                _socket.Listen(99);
                _socket.BeginAccept(AccCallBack, _socket);
            }
            catch (Exception exp)
            {
                throw new Exception($"{GetType().Name}>StartServer", exp);
            }
        }

        public void AccCallBack(IAsyncResult asr)
        {
            try
            {
                Socket AccSocket = _socket.EndAccept(asr);
                //نگهداری کانکشن کلاینت ها در یک لیست قابل دسترس
                int _clientId = ClsClients.AddClient(AccSocket);
                Console.WriteLine($"Accept new Client[{_clientId}]");
                //اماده پذیرش کلاینت جدید
                _socket.BeginAccept(AccCallBack, _socket); 
            }
            catch (Exception exp)
            {
                throw new Exception($"{GetType().Name}>AccCallBack", exp);
            }
        }
    }

    //موجودیت هر کلاینت و استقلال تبادل پیام
    class ClsClient
    {
        public Socket Socket { get; set; }
        public ClsReceiveMsg Receive { get; set; }
        public int Id { get; set; }
        public ClsClient(Socket socket, int id)
        {
            Receive = new ClsReceiveMsg(socket, id);
            Receive.StartReceiving();
            Socket = socket;
            Id = id;
        }
    }

    public class ClsReceiveMsg
    {
        Socket _recSocket;
        byte[] _packetbuffersize;
        int _clientId;
        ClsSendMsg _sendmsg;

        public ClsReceiveMsg(Socket ReceiveSocket, int ClientId)
        {
            _recSocket = ReceiveSocket;
            _clientId = ClientId;
            _sendmsg = new ClsSendMsg(_recSocket);
        }

        public void StartReceiving()
        {
            try
            {
                //چهاربایت اول پیام طول پیام را مشخص میکند
                _packetbuffersize = new byte[4];
                _recSocket.BeginReceive(_packetbuffersize, 0, _packetbuffersize.Length, SocketFlags.None, ReceiveCallback, null);
            }
            catch (Exception exp)
            {
                throw new Exception($"{GetType().Name}>StartReceiving", exp);
            }
        }

        private void ReceiveCallback(IAsyncResult acr)
        {
            try
            {
                if (_recSocket.EndReceive(acr) > 1)
                {
                    _packetbuffersize = new byte[BitConverter.ToInt32(_packetbuffersize, 0)];
                    _recSocket.Receive(_packetbuffersize, _packetbuffersize.Length, SocketFlags.None);

                    string ReceivePacket = Encoding.Default.GetString(_packetbuffersize);
                    //تبدیل پکیت دریافتی به موجودیت پیام
                    ClsMessage msg = JsonConvert.DeserializeObject<ClsMessage>(ReceivePacket);
                    Console.WriteLine($"Receive Msg From [{_clientId}] :{msg.Data}");
                    switch (msg.Data.ToLower())
                    {
                        case "hello":
                            _sendmsg.Send(JsonConvert.SerializeObject(new ClsMessage("Hi")));
                        break;
                        case "bye":
                            _sendmsg.Send(JsonConvert.SerializeObject(new ClsMessage("Bye")));
                            break;
                        case "ping":
                            _sendmsg.Send(JsonConvert.SerializeObject(new ClsMessage("Pong")));
                            break;
                        default:
                            //Send Exception Error
                            // ارسال پیام خطا به سمت کاربر
                            _sendmsg.Send(JsonConvert.SerializeObject(new ClsMessage("Out Of Range Message","err")));
                            break;
                    }


                    StartReceiving();
                }
                else
                {
                    Disconnect();
                }
            }
            catch
            {
                if (!_recSocket.Connected)
                    Disconnect();
                else
                    StartReceiving();
            }
        }

        private void Disconnect()
        {
            _recSocket.Disconnect(true);
            ClsClients.RemoveClient(_clientId);
            Console.WriteLine($"Disconnect Client:[{_clientId}]  :(");
        }
    }

    //کلاس ارسال کننده پیام
    public class ClsSendMsg
    {
        Socket _socked;

        public ClsSendMsg(Socket Socket)
        {
            _socked = Socket;
        }

        public void Send(string data)
        {
            try
            {
                var _packet = new List<byte>();
                //اندیس اول نگهدارنده طول پیام ارسال
                _packet.AddRange(BitConverter.GetBytes(data.Length));
                //اندیس دوم شامل پیام ارسالی
                _packet.AddRange(Encoding.Default.GetBytes(data));
                _socked.Send(_packet.ToArray());
            }
            catch (Exception exp)
            {
                throw new Exception($"{GetType().Name}>Send", exp);
            }
        }
    }

    //موجودیت پیام
    class ClsMessage
    {
        /// <summary>
        /// نوع پیام
        /// err : خطا
        /// msg : پیام عادی
        /// </summary>
        public string Type { get; set; }
        public string Data { get; set; }
        public ClsMessage(string Data, string Type ="msg")
        {
            this.Data = Data;
            this.Type = Type.ToLower();
        }
    }
}
