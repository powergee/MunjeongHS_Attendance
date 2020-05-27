using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Reflection;
using System.Threading;

namespace 문정고등학교_출석부_Server_Starter
{
    class Program
    {
        public static readonly string DIRECTORY = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        public static readonly string EXE_NAME = "문정고등학교 출석부 Server.exe";

        static void Main(string[] args)
        {
            while (true)
            {
                try
                {
                    Console.WriteLine($"[{DateTime.Now.ToString()}] 서버를 시작합니다...");
                    Process server = Process.Start($"{DIRECTORY}\\{EXE_NAME}");

                    if (server.IsRunning())
                        Console.WriteLine($"[{DateTime.Now.ToString()}] 서버를 시작하였습니다.");
                    else
                    {
                        Console.WriteLine($"[{DateTime.Now.ToString()}] 서버를 시작하지 못하였습니다. 1초 뒤에 다시 시도합니다.");
                        Thread.Sleep(1000);
                        continue;
                    }

                    server.WaitForExit();
                    Console.WriteLine($"[{DateTime.Now.ToString()}] 서버가 종료되었음을 감지하였습니다. 1초 뒤에 서버를 다시 시작합니다.");
                    Thread.Sleep(1000);

                }
                catch (Exception e)
                {
                    Console.WriteLine($"[{DateTime.Now.ToString()}] 예외 발생! : {e.Message} \n {e.StackTrace}");
                }
            }
        }
    }

    public static class processExtensions
    {
        public static bool IsRunning(this Process process)
        {
            if (process == null)
                throw new ArgumentNullException("process");

            try
            {
                Process.GetProcessById(process.Id);
            }
            catch (ArgumentException)
            {
                return false;
            }

            return true;
        }
    }
}
