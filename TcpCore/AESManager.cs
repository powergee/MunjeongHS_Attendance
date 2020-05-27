using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security;
using System.Security.Cryptography;
using System.IO;

namespace TcpCore
{
    public class AESManager : IDisposable
    {
        private AesCryptoServiceProvider mAESProvider;

        public AESKeyIVPair KeyIVPair => new AESKeyIVPair(mAESProvider.Key, mAESProvider.IV);

        public AESManager()
        {
            mAESProvider = new AesCryptoServiceProvider();
            mAESProvider.Padding = PaddingMode.ISO10126;
        }

        public AESManager(AESKeyIVPair pair)
        {
            mAESProvider = new AesCryptoServiceProvider();
            mAESProvider.Padding = PaddingMode.ISO10126;
            mAESProvider.Key = pair.Key;
            mAESProvider.IV = pair.IV;
        }

        public byte[] Encrypt(byte[] data)
        {
            return Encrypt(data, 0, data.Length);
        }

        public byte[] Encrypt(byte[] data, int offset, int count)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            if (data.Length == 0)
                throw new ArgumentException("AES 암호화하려는 데이터의 길이가 0입니다.");
            if (offset < 0 || offset >= data.Length)
                throw new ArgumentOutOfRangeException("offset");
            if (offset + count > data.Length)
                throw new ArgumentOutOfRangeException("count");

            byte[] part;

            if (offset == 0 && count == data.Length)
                part = data;
            else
            {
                part = new byte[count];

                for (int i = 0; i < count; ++i)
                {
                    part[i] = data[i + offset];
                }
            }

            byte[] result;

            using (ICryptoTransform encryptor = mAESProvider.CreateEncryptor())
            {
                result = encryptor.TransformFinalBlock(data, offset, count);
            }

            return result;
        }

        public ICryptoTransform CreateCryptoTransform(AESCrypto cryptoEnum)
        {
            switch (cryptoEnum)
            {
                case AESCrypto.Encrypt:
                    return mAESProvider.CreateEncryptor();
                case AESCrypto.Decrypt:
                    return mAESProvider.CreateDecryptor();

                default:
                    throw new ArgumentException("AESCrypto 열거형 값이 올바르지 않습니다.");
            }
        }

        /*public AESCryptoSet CreateCryptoSet(AESCrypto cryptoEnum)
        {
            return new AESCryptoSet(CreateCryptoTransform(cryptoEnum));
        }*/

        public byte[] Decrypt(byte[] data)
        {
            return Decrypt(data, 0, data.Length);
        }

        public byte[] Decrypt(byte[] data, int offset, int count)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            if (data.Length == 0)
                throw new ArgumentException("AES 복호화하려는 데이터의 길이가 0입니다.");
            if (offset < 0 || offset >= data.Length)
                throw new ArgumentOutOfRangeException("offset");
            if (offset + count > data.Length)
                throw new ArgumentOutOfRangeException("count");

            byte[] part;

            if (offset == 0 && count == data.Length)
                part = data;
            else
            {
                part = new byte[count];

                for (int i = 0; i < count; ++i)
                {
                    part[i] = data[i + offset];
                }
            }

            byte[] result;

            using (ICryptoTransform decryptor = mAESProvider.CreateDecryptor())
            {
                result = decryptor.TransformFinalBlock(data, offset, count);
            }

            return result;
        }

        #region IDisposable Support
        private bool disposedValue = false; // 중복 호출을 검색하려면

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    mAESProvider.Dispose();
                }

                // TODO: 관리되지 않는 리소스(관리되지 않는 개체)를 해제하고 아래의 종료자를 재정의합니다.
                // TODO: 큰 필드를 null로 설정합니다.

                disposedValue = true;
            }
        }

        // TODO: 위의 Dispose(bool disposing)에 관리되지 않는 리소스를 해제하는 코드가 포함되어 있는 경우에만 종료자를 재정의합니다.
        // ~AESManager() {
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

    /*public class AESCryptoSet : IDisposable
    {
        private ICryptoTransform mTransform;
        private CryptoStream mCStream;
        private MemoryStream mMStream;

        public int LatestProcessedLength { get; private set; }

        public AESCryptoSet(ICryptoTransform transform)
        {
            mTransform = transform ?? throw new ArgumentNullException("transform");
            mMStream = new MemoryStream();
            mCStream = new CryptoStream(mMStream, mTransform, CryptoStreamMode.Write);
        }

        public void Process(byte[] data)
        {
            Process(data, 0, data.Length);
        }

        public void Process(byte[] data, int offset, int count)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            if (data.Length == 0)
                throw new ArgumentException("AESCryptoSet에서 처리하려는 데이터의 길이가 0입니다.");
            if (offset < 0 || offset >= data.Length)
                throw new ArgumentOutOfRangeException("offset");
            if (offset + count > data.Length)
                throw new ArgumentOutOfRangeException("count");

            mMStream.Position = 0;
            
            mCStream.Write(data, offset, count);
            long afterPos = mMStream.Position;

            LatestProcessedLength = Convert.ToInt32(afterPos);
        }

        public void GetLatest(byte[] buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            if (buffer.Length < LatestProcessedLength)
                throw new ArgumentException("buffer의 길이가 너무 짧아 데이터를 다 담을 수 없습니다.");

            mMStream.Position -= LatestProcessedLength;
            mMStream.Read(buffer, 0, LatestProcessedLength);
        }

        public void FlushFinalBlock()
        {
            if (!mCStream.HasFlushedFinalBlock)
            {
                long beforePos = mMStream.Position;
                mCStream.FlushFinalBlock();
                long afterPos = mMStream.Position;

                LatestProcessedLength += Convert.ToInt32(afterPos - beforePos);
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // 중복 호출을 검색하려면

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 관리되는 상태(관리되는 개체)를 삭제합니다.
                    mCStream.Dispose();
                    mMStream.Dispose();
                    mTransform.Dispose();
                }

                // TODO: 관리되지 않는 리소스(관리되지 않는 개체)를 해제하고 아래의 종료자를 재정의합니다.
                // TODO: 큰 필드를 null로 설정합니다.

                disposedValue = true;
            }
        }

        // 삭제 가능한 패턴을 올바르게 구현하기 위해 추가된 코드입니다.
        public void Dispose()
        {
            // 이 코드를 변경하지 마세요. 위의 Dispose(bool disposing)에 정리 코드를 입력하세요.
            Dispose(true);
            // TODO: 위의 종료자가 재정의된 경우 다음 코드 줄의 주석 처리를 제거합니다.
            // GC.SuppressFinalize(this);
        }
        #endregion

    }*/

    public class AESKeyIVPair
    {
        public byte[] Key { get; }
        public byte[] IV { get; }

        public AESKeyIVPair(byte[] key, byte[] iv)
        {
            Key = key ?? throw new ArgumentNullException("key");
            IV = iv ?? throw new ArgumentNullException("iv");
        }

        public override string ToString()
        {
            return $"{Convert.ToBase64String(Key)} / {Convert.ToBase64String(IV)}";
        }
    }

    public enum AESCrypto { Encrypt, Decrypt }
}
