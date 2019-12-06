using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Client
{
    class ClsClient
    {
        Socket _socket;
        const int _port = 1111;
        const string _serverIp = "127.0.0.1";
        ClsSendMsg _sender;

        public bool ConnectionState { get { return _socket.Connected; } }

        public void Connect()
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            while (!_socket.Connected)
            {
                Thread.Sleep(1000);
                try
                {
                    _socket.Connect(new IPEndPoint(IPAddress.Parse(_serverIp), _port));
                }
                catch { }
            }
            Console.WriteLine($"Connect to server :)");
            SetupForReceiveing();
        }
        private void SetupForReceiveing()
        {
            ClsReceiveMsg Receiver = new ClsReceiveMsg(_socket);
            _sender = new ClsSendMsg(_socket);
            Receiver.StartReceiving();
        }

        public void SendMsgToServer(string message)
        {
            if (_socket.Connected && _sender != null)
                _sender.Send(message);
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

    public class ClsReceiveMsg
    {
        Socket _recSocket;
        byte[] _packetbuffersize;

        public ClsReceiveMsg(Socket ReceiveSocket)
        {
            _recSocket = ReceiveSocket;
        }

        public void StartReceiving()
        {
            try
            {
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
                //اگر داده با طول صفر ارسال شود ارتباط قطع می شود
                //در غیر اینصورت همواره تلاش میشود ارتباط پایدار بماند
                if (_recSocket.EndReceive(acr) > 1)
                {
                    _packetbuffersize = new byte[BitConverter.ToInt32(_packetbuffersize, 0)];
                    _recSocket.Receive(_packetbuffersize, _packetbuffersize.Length, SocketFlags.None);

                    string data = Encoding.Default.GetString(_packetbuffersize);
                    ClsMessage msg = JsonConvert.DeserializeObject<ClsMessage>(data);
                    if (msg.Type == "err")
                    {
                        //دریافت پیام خطا از سمت سرور
                        //throw new Exception($"Error From Server : {msg.Data}");
                        Console.WriteLine($"Error From Server : {msg.Data}");
                        Disconnect();
                        return;
                    }
                    else
                        Console.WriteLine($"Receive Msg From Server : {msg.Data}");

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
            Console.WriteLine($"Disconnect of Server");
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
        public ClsMessage(string Data, string Type = "msg")
        {
            this.Data = Data;
            this.Type = Type.ToLower();
        }
    }
}
