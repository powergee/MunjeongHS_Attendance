using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using TcpCore;
using System.Security;
using System.Security.Cryptography;
using System.IO;

namespace ServerTest
{
    class Program
    {
        public static readonly string PATH = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), "TestSending.zip");

        static EventServer mServer;

        static void Main(string[] args)
        {
            Console.WriteLine($"테스트 전송할 파일 경로 : {PATH}");

            Debug.MessageWritten += Debug_MessageWritten;

            //DatabaseManager.Initialize();

            mServer = new EventServer(31006);
            mServer.ClientConnected += Server_ClientConnected;
            mServer.Start();

            Console.WriteLine("서버가 시작되었습니다. 31006 포트");

            Console.WriteLine("아무키나 누르면 클라이언트를 종료합니다...");
            Console.ReadKey(false);
            mServer.Halt();
        }

        private static void Debug_MessageWritten(object sender, MessageWrittenEventArgs e)
        {
            switch (e.Type)
            {
                case DebugMessageType.Error:
                    Console.WriteLine($"ERROR : {e.Message}");
                    break;
                case DebugMessageType.General:
                    Console.WriteLine($"GENERAL : {e.Message}");
                    break;
            }
        }

        private static void Server_ClientConnected(object sender, ClientConnectedEventArgs e)
        {
            Console.WriteLine($"클라이언트가 연결되었습니다. {e.Client.ConnectedTime}, {e.Client.IP}");
            e.Client.Disconnected += Client_Disconnected;
            e.Client.ReceivedMessage += Client_ReceivedMessage;

            e.Client.SendMessage("Hello, Client!");

            /*Console.WriteLine($"{e.Client.IP}에 테스트 파일 전송을 시작합니다.");
            var notifier = e.Client.StartSendingFile(PATH);
            var client = e.Client;

            ThreadPool.QueueUserWorkItem((o) => {
                while (!notifier.IsFinished && client.IsOnline)
                {
                    Console.WriteLine($"파일 전송 중... {notifier.Percent} %");
                    Thread.Sleep(1000);
                }
            });*/
        }

        private static void Client_ReceivedMessage(object sender, ReceivedMessageEventArgs e)
        {
            EventClient client = sender as EventClient;
            Console.WriteLine($"[{DateTime.Now.ToString()}] {client.IP} : {e.Message}");
        }

        private static void Client_Disconnected(object sender, DisconnectedEventArgs e)
        {
            EventClient client = sender as EventClient;
            Console.WriteLine($"클라이언트와의 연결이 끊어졌습니다. {client.DisconnectedTime.Value}, {client.IP}");
            Console.WriteLine($"클라이언트 수 : {mServer.Clients.Count}");
        }
    }
}
