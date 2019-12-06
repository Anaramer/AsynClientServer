using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            ClsClient client = new ClsClient();
            client.Connect();
            Console.WriteLine("Enter Your Message & Press [Enter Key]");
            while (client.ConnectionState)
            {
                string Msg = Console.ReadLine();
                client.SendMsgToServer(JsonConvert.SerializeObject(new ClsMessage(Msg)));
            }
        }
    }
}
