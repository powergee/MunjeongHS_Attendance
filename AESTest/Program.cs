using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TcpCore;
using System.IO;
using System.Threading;
using System.Security.Cryptography;

namespace AESTest
{
    class Program
    {
        static readonly string DIR_PATH = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
        static readonly string SEND_PATH = Path.Combine(DIR_PATH, "TestSending.zip");
        static readonly string RECEIVE_PATH = Path.Combine(DIR_PATH, "TestReceiving.zip");

        static void Main(string[] args)
        {
            /*AesCryptoServiceProvider aes = new AesCryptoServiceProvider();
            ICryptoTransform encryptor = aes.CreateEncryptor();
            MemoryStream ms = new MemoryStream();
            CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);

            cs.Dispose();

            try
            {
                ms.Write(new byte[] { 0, 0, 0, 0 }, 0, 4);
            }
            catch (Exception e)
            {
                Console.WriteLine($"1. {e.Message}");
            }

            try
            { 
                encryptor.TransformFinalBlock(new byte[] { 0, 0, 0, 0 }, 0, 4);
            }
            catch (Exception e)
            {
                Console.WriteLine($"2. {e.Message}");
            }

            try
            {
                aes.CreateDecryptor();
            }
            catch (Exception e)
            {
                Console.WriteLine($"3. {e.Message}");
            }*/

            byte[] buffer = new byte[81920];
            byte[] decrypted;
            DateTime printed = DateTime.Now;
            TimeSpan printSpan = new TimeSpan(0, 0, 1);
            int read;

            using (FileStream sStream = File.OpenRead(SEND_PATH))
            using (FileStream rStream = File.Create(RECEIVE_PATH))
            using (AESManager aes1 = new AESManager())
            using (AESManager aes2 = new AESManager(aes1.KeyIVPair))
            {
                while (sStream.Position != sStream.Length)
                {
                    read = sStream.Read(buffer, 0, 81920);
                    decrypted = aes2.Decrypt(aes1.Encrypt(buffer));

                    rStream.Write(decrypted, 0, read);

                    if (DateTime.Now - printed > printSpan)
                    {
                        Console.WriteLine($"{sStream.Position}/{sStream.Length} ({(double)sStream.Position / (double)sStream.Length}) 완료...");
                        printed = DateTime.Now;
                    }
                }
            }

            Console.WriteLine("파일이 손상없이 암/복호화되었는지 확인합니다...");

            FileInfo sent = new FileInfo(SEND_PATH);
            FileInfo received = new FileInfo(RECEIVE_PATH);

            if (sent.Length != received.Length)
            {
                Console.WriteLine($"파일의 길이가 다릅니다. (전송한 파일 : {sent.Length}, 받은 파일 : {received.Length})");
                return;
            }
            byte[] sBuffer = new byte[81920], rBuffer = new byte[81920];

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

            Console.ReadKey(false);
        }
    }
}
