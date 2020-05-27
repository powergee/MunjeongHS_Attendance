using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security;
using System.Security.Cryptography;

namespace TcpCore
{
    public class RSAManager : IDisposable
    {
        RSAParameters mPrivateKey;
        RSAParameters mPublicKey;

        RSACryptoServiceProvider mEncryptor;
        RSACryptoServiceProvider mDecryptor;

        public string PublicKeyXML => mEncryptor.ToXmlString(false);

        public RSAManager()
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048);

            mPrivateKey = rsa.ExportParameters(true);
            mPublicKey = rsa.ExportParameters(false);

            mEncryptor = new RSACryptoServiceProvider(2048);
            mEncryptor.ImportParameters(mPublicKey);
            mDecryptor = new RSACryptoServiceProvider(2048);
            mDecryptor.ImportParameters(mPrivateKey);

            rsa.Dispose();
        }

        public byte[] Encrypt(byte[] data)
        {
            return mEncryptor.Encrypt(data, false);
        }

        public static byte[] Encrypt(byte[] data, string publicKeyXML)
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(publicKeyXML);
            byte[] buffer = rsa.Encrypt(data, false);
            rsa.Dispose();

            return buffer;
        }

        public byte[] Decrypt(byte[] data)
        {
            return mDecryptor.Decrypt(data, false);
        }

        #region IDisposable Support
        private bool disposedValue = false; // 중복 호출을 검색하려면

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    mEncryptor.Dispose();
                    mDecryptor.Dispose();
                }

                // TODO: 관리되지 않는 리소스(관리되지 않는 개체)를 해제하고 아래의 종료자를 재정의합니다.
                // TODO: 큰 필드를 null로 설정합니다.

                disposedValue = true;
            }
        }

        // TODO: 위의 Dispose(bool disposing)에 관리되지 않는 리소스를 해제하는 코드가 포함되어 있는 경우에만 종료자를 재정의합니다.
        // ~RSAManager() {
        //   // 이 코드를 변경하지 마세요. 위의 Dispose(bool disposing)에 정리 코드를 입력하세요.
        //   Dispose(false);
        // }

        // 삭제 가능한 패턴을 올바르게 구현하기 위해 추가된 코드입니다.
        public void Dispose()
        {
            // 이 코드를 변경하지 마세요. 위의 Dispose(bool disposing)에 정리 코드를 입력하세요.
            Dispose(true);
            // TODO: 위의 종료자가 재정의된 경우 다음 코드 줄의 주석 처리를 제거합니다.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
