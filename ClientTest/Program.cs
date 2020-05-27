using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TcpCore;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;

namespace ClientTest
{
    class Program
    {
        static string h_IP = "124.53.246.81";
        static string c_IP = "192.168.219.142";
        static string r_IP = "192.168.219.186";
        static string l_IP = "127.0.0.1";

        static int loginCount = 0;

        static void Main(string[] args)
        {
            Debug.MessageWritten += Debug_MessageWritten;

            Console.Write("연결할 서버 IP 입력 : ");
            string input = Console.ReadLine();
            string ip;

            if (input == "h")
                ip = h_IP;
            else if (input == "c")
                ip = c_IP;
            else if (input == "r")
                ip = r_IP;
            else if (input == "l")
                ip = l_IP;
            else ip = input;

            TcpClient lowClient = new TcpClient(ip, 31006);
            EventClient client = new EventClient(lowClient,  false);

            client.ReceivedMessage += Client_ReceivedMessage;
            client.FileReceivingStarted += Client_FileReceivingStarted;
            client.FileReceivingEnd += Client_FileReceivingEnd;
            client.ReceivedLoginResult += Client_ReceivedLoginResult;

            client.SendMessage("Hello, Server!");

            Console.WriteLine("3초 뒤 맥주 배달...");
            Thread.Sleep(3000);
            SendBeer(99, client);
            Console.WriteLine("맥주 배달 완료!");

            /*client.RequestLogin("wrong", "123qweasd");
            client.RequestLogin("root", "wrong");
            client.RequestLogin("root", "123qweasd");

            TcpDataInsert insert = new TcpDataInsert();
            List<DatabaseValues> values = new List<DatabaseValues>();

            values.Add(new DatabaseValues("#2000-8-14#", "99".ToSQLString(), "99".ToSQLString(), "99".ToSQLString(), "김정현".ToSQLString(), "1", "1", "1", "1", "1", "1", "1", "1", "1"));
            values.Add(new DatabaseValues("#2000-8-14#", "99".ToSQLString(), "99".ToSQLString(), "100".ToSQLString(), "김정현".ToSQLString(), "1", "1", "1", "1", "1", "1", "1", "1", "1"));
            values.Add(new DatabaseValues("#2000-8-14#", "99".ToSQLString(), "99".ToSQLString(), "101".ToSQLString(), "김정현".ToSQLString(), "1", "1", "1", "1", "1", "1", "1", "1", "1"));

            insert.ColumnsText = "";
            insert.TableName = "출석부";
            insert.Values = values;

            client.InsertData(insert);

            TcpDataDelete delete = new TcpDataDelete();
            delete.TableName = "[출석부]";
            delete.Where = "[일자]=#2000-8-14# AND 학년=\"99\" AND 반=\"99\" AND 번호=\"99\"";

            client.DeleteData(delete);*/

            Console.WriteLine("아무키나 누르면 클라이언트를 종료합니다...");
            Console.ReadKey(false);
            client.Disconnect();
        }

        private static void Client_ReceivedLoginResult(object sender, ReceivedLoginResultEventArgs e)
        {
            Console.WriteLine($"{++loginCount}번째 로그인 : {e.Result.ToString()}");
        }

        private static void Client_FileReceivingEnd(object sender, FileReceivingEndEventArgs e)
        {
            Console.WriteLine("테스트 파일 수신이 끝났습니다.");

            e.Stream.Close();

            Console.WriteLine("파일이 손상없이 전송되었는지 확인합니다...");

            FileInfo sent = new FileInfo(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), "TestSending.zip"));
            FileInfo received = new FileInfo(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), "TestReceiving.zip"));

            if (sent.Length != received.Length)
            {
                Console.WriteLine($"파일의 길이가 다릅니다. (전송한 파일 : {sent.Length}, 받은 파일 : {received.Length})");
                return;
            }

            ThreadPool.QueueUserWorkItem((o) =>
            {
                byte[] sBuffer = new byte[81920], rBuffer = new byte[81920];
                int read;

                try
                {
                    using (FileStream sStream = sent.OpenRead())
                    using (FileStream rStream = received.OpenRead())
                    {
                        while (sStream.Position != sStream.Length)
                        {
                            sStream.Read(sBuffer, 0, 81920);
                            read = rStream.Read(rBuffer, 0, 81920);

                            for (int i = 0; i < read; ++i)
                            {
                                if (sBuffer[i] != rBuffer[i])
                                    throw new Exception($"{(sStream.Position - read + i)}에서 ({sStream.Position - 81920} ~ {sStream.Position}) 불일치를 발견하였습니다. (전체 길이 {sent.Length})");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    return;
                }

                Console.WriteLine("파일이 일치합니다.");
            });
        }

        private static void Client_FileReceivingStarted(object sender, FileReceivingStartedEventArgs e)
        {
            Console.WriteLine("테스트 파일 수신을 시작합니다.");

            FileStream fs = new FileStream(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), "TestReceiving.zip"), FileMode.Create);
            e.Stream = fs;

            TcpTransferNotifier notifier = e.Notifier;

            ThreadPool.QueueUserWorkItem((o) => {
                while (!notifier.IsFinished && (sender as EventClient).IsOnline)
                {
                    Console.WriteLine($"파일 수신 중... {notifier.Percent} %");
                    Thread.Sleep(1000);
                }
            });
        }

        private static void Client_ReceivedMessage(object sender, ReceivedMessageEventArgs e)
        {
            EventClient client = sender as EventClient;
            Console.WriteLine($"{client.IP}로부터 메세지 수신함 : {e.Message}");
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

        private static void SendBeer(int bottlesNum, EventClient client)
        {
            for (int i = bottlesNum; i >= 0; --i)
            {
                if (i == 0)
                {
                    client.SendMessage("No more bottles of beer on the wall, no more bottles of beer.");
                    client.SendMessage($"Go to the store and buy some more, {BottlesToString(bottlesNum)} of beer on the wall.");
                }

                else
                {
                    string bottlesStr = BottlesToString(i);
                    client.SendMessage($"{bottlesStr} of beer on the wall, {bottlesStr} of beer.");
                    client.SendMessage($"Take one down and pass it around, {BottlesToString(i - 1)} of beer on the wall.");
                }
            }
        }

        private static string BottlesToString(int bottles)
        {
            if (bottles == 0)
                return "no more bottles";

            if (bottles == 1)
                return "1 bottle";

            return $"{bottles} bottles";
        }
    }
}
