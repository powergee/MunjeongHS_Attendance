using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;

namespace TcpCore
{
    public class EventClient : IDisposable, INotifyPropertyChanged
    {
        private readonly static TimeSpan PING_SPAN = new TimeSpan(0, 0, 0, 5, 0);
        private readonly static TimeSpan NO_RESPONSE_SPAN = new TimeSpan(0, 0, 0, 10, 0);
        private readonly static int FILE_TRANS_SIZE = 81920;

        private RSAManager mRSA;
        private AESManager mAES;
        private TcpClient mLowClient;
        private NetworkStream mNetStream;
        // 아래 두 변수의 동사 (Receive, Send)의 주체는 이 객체를 생성한 서버임. (클라이언트는 이 변수를 사용하지 않음.)
        private DateTime mLastPingReceive;
        private DateTime mLastPingSend;

        private bool mFileReceiving = false;
        private TcpTransferNotifier mFileReceivingNotifier = null;
        private TcpFileInfo mFileReceivingInfo = null;
        private Stream mFileReceiveStream = null;

        private bool mFileSending = false;
        private TcpTransferNotifier mFileSendingNotifier = null;

        public DateTime ConnectedTime { get; private set; }
        public DateTime? DisconnectedTime { get; private set; } = null;
        public IPAddress IP { get; private set; }
        public bool IsOnline { get; private set; }
        public bool DoesSendPing { get; }
        public bool FileTransfering => mFileReceiving || mFileSending;

        public bool LoginAccepted { get; private set; } = false;
        public string AccountID { get; private set; } = "-";
        public string AccountName { get; private set; } = "-";
        public string AccountType { get; private set; } = "-";

        public event EventHandler<DisconnectedEventArgs> Disconnected;
        public event EventHandler<ReceivedMessageEventArgs> ReceivedMessage;
        public event EventHandler<FileReceivingStartedEventArgs> FileReceivingStarted;
        public event EventHandler<FileReceivingEndEventArgs> FileReceivingEnd;
        public event EventHandler<ReceivedLoginResultEventArgs> ReceivedLoginResult;
        public event EventHandler<ReceivedDataSetResultEventArgs> ReceivedDataSetResult;
        public event EventHandler<InsertedDataEventArgs> InsertedData;
        public event EventHandler<ReceivedRegisterResultEventArgs> ReceivedRegisterResult;
        public event PropertyChangedEventHandler PropertyChanged;


        // isServer : EventClient를 생성하는 프로세스가 서버인지를 나타냄. false일 경우 Ping을 먼저 보내지 않음.
        public EventClient(TcpClient client, bool isServer)
        {
            if (client == null)
            {
                throw new ArgumentNullException("client");
            }

            mLowClient = client;
            mNetStream = client.GetStream();
            ConnectedTime = DateTime.Now;
            mLastPingReceive = mLastPingSend = ConnectedTime;
            DoesSendPing = isServer;
            IP = ((IPEndPoint)client.Client.RemoteEndPoint).Address;
            Debug.Message($"EventClient의 생성자에서 프로퍼티를 할당하였음 : {ConnectedTime}, {IP}");

            Debug.Message($"{IP}와 연결 준비 중...");
            IsOnline = true;

            bool cryptoSuccess = false;
            string exceptionMessage = "타임 아웃";
            Task.Run(() =>
            {
                try
                {
                    if (isServer)
                    {
                        // 1. 서버가 RSA 공개키를 클라이언트에 전송함.
                        Debug.Message($"{IP}에 RSA 공개키 전송...");
                        mRSA = new RSAManager();
                        byte[] pubKey = Encoding.UTF8.GetBytes(mRSA.PublicKeyXML);
                        Packet rsaPacket = new Packet(PacketType.RSAPublic, pubKey.Length);
                        mNetStream.Write(rsaPacket.ToBytes(), 0, Packet.SIZE);
                        mNetStream.Write(pubKey, 0, pubKey.Length);
                        Debug.Message($"전송 완료.");

                        // 4. 클라이언트가 보낸 암호화된 AES 비밀키(Key, IV)를 수신하고 AES 개체를 초기화함.
                        Debug.Message($"{IP}로 부터 AES 정보 수신...");
                        Packet aesPacket = ReceivePacket(PacketType.AESKey);
                        byte[] enAESKey = ReceiveCompletely(aesPacket.DataLength);

                        aesPacket = ReceivePacket(PacketType.AESIV);
                        byte[] enAESIV = ReceiveCompletely(aesPacket.DataLength);

                        AESKeyIVPair pair = new AESKeyIVPair(mRSA.Decrypt(enAESKey), mRSA.Decrypt(enAESIV));
                        mAES = new AESManager(pair);
                        Debug.Message($"수신 완료.");
                    }
                    else
                    {
                        // 2. 서버가 보낸 RSA 공개키를 수신함.
                        Debug.Message($"{IP}로 부터 RSA 공개키 수신...");
                        Packet rsaPacket = ReceivePacket(PacketType.RSAPublic);
                        byte[] pubKey = ReceiveCompletely(rsaPacket.DataLength);
                        string pubKeyXML = Encoding.UTF8.GetString(pubKey);
                        Debug.Message($"수신 완료.");

                        // 3. 수신한 공개키를 이용해 AES 비밀키(Key, IV)를 암호화하여 서버에 전송함.
                        Debug.Message($"{IP}에 AES 정보 전송...");
                        mAES = new AESManager();
                        AESKeyIVPair pair = mAES.KeyIVPair;

                        byte[] enKey = RSAManager.Encrypt(pair.Key, pubKeyXML);
                        byte[] enIV = RSAManager.Encrypt(pair.IV, pubKeyXML);

                        Packet aesKeyPacket = new Packet(PacketType.AESKey, enKey.Length);
                        Packet aesIVPacket = new Packet(PacketType.AESIV, enIV.Length);

                        mNetStream.Write(aesKeyPacket.ToBytes(), 0, Packet.SIZE);
                        mNetStream.Write(enKey, 0, enKey.Length);

                        mNetStream.Write(aesIVPacket.ToBytes(), 0, Packet.SIZE);
                        mNetStream.Write(enIV, 0, enIV.Length);
                        Debug.Message($"전송 완료.");
                    }

                    cryptoSuccess = true;
                }
                catch (Exception e)
                {
                    exceptionMessage = e.Message;
                    cryptoSuccess = false;
                }
            }).Wait(1000*60);

            if (!cryptoSuccess)
            {
                Debug.Error($"상대와 연결하는데 실패하였습니다 : {exceptionMessage}");
                this.Disconnect();
                return;
            }

            //Debug.Message($"AES Key/IV = {mAES.KeyIVPair.ToString()}");

            ThreadPool.QueueUserWorkItem(ThreadLoop);
        }

        private byte[] ReceiveCompletely(int length)
        {
            byte[] result = new byte[length];

            int pos = 0, read;

            while (pos < length)
            {
                read = mNetStream.Read(result, pos, length - pos);
                pos += read;
            }

            return result;
        }

        private void ThreadLoop(object o)
        {
            try
            {
                while (IsOnline)
                {
                    if (!mLowClient.Connected)
                    {
                        Debug.Message($"클라이언트 {IP} 소켓의 Connected 가 false 이므로 연결을 종료합니다.");
                        Disconnect();
                        break;
                    }

                    // 서버만 이 코드 실행
                    if (DoesSendPing && !FileTransfering)
                    {
                        // 아래 두 변수의 동사 (Send, Receive)의 주체는 서버임.
                        TimeSpan noPingSendSpan = DateTime.Now - mLastPingReceive;
                        TimeSpan noPingReceiveSpan = DateTime.Now - mLastPingSend;
                        bool waitingForResponse = DateTime.Compare(mLastPingReceive, mLastPingSend) < 0;

                        // 핑을 보내야 할 때
                        if (noPingSendSpan > PING_SPAN && !waitingForResponse)
                        {
                            SendPing();
                        }
                        // 핑 타임아웃
                        else if (noPingReceiveSpan > NO_RESPONSE_SPAN && waitingForResponse)
                        {
                            Debug.Error($"클라이언트 {IP}가 핑에 응답하지 않습니다. 연결을 종료합니다.");
                            Disconnect();
                            break;
                        }
                    }

                    // 여기서 수신된 데이터(상대가 보낸 핑 포함)를 처리
                    if (mLowClient.Available > 0)
                    {
                        Packet packet = ReceivePacket();

                        switch (packet.Type)
                        {
                            case PacketType.Message:
                                HandleMessage(packet.DataLength);
                                break;

                            case PacketType.Ping:
                                HandlePing();
                                break;

                            case PacketType.FileSendStart:
                                #region FileSendStart
                                if (mFileReceiving)
                                    throw new InvalidOperationException("파일을 받는 도중 새로운 파일 전송 시작을 알리는 패킷이 도착하였습니다.");

                                mFileReceiving = true;
                                OnPropertyChanged("FileTransfering");
                                byte[] infoBuffer = ReceiveCompletely(packet.DataLength);

                                TcpFileInfo fileInfo = TcpFileInfo.Deserialize(infoBuffer);
                                mFileReceivingNotifier = new TcpTransferNotifier(fileInfo.Length);
                                var e = new FileReceivingStartedEventArgs(fileInfo, mFileReceivingNotifier);
                                FileReceivingStarted?.Invoke(this, e);

                                if (e.Stream == null || !e.Stream.CanWrite)
                                    throw new InvalidOperationException("파일 수신 시작 이벤트에서 설정한 스트림이 null 이거나 쓸 수 없습니다.");

                                mFileReceiveStream = e.Stream;
                                mFileReceivingInfo = fileInfo;
                                #endregion
                                break;

                            case PacketType.FileSending:
                                #region FileSending
                                if (!mFileReceiving)
                                    throw new InvalidOperationException("파일을 받는 중이 아닌 시점에 파일 전송 패킷이 도착하였습니다.");

                                byte[] buffer = ReceiveCompletely(packet.DataLength);
                                byte[] decrypted = mAES.Decrypt(buffer);
                                
                                mFileReceiveStream.Write(decrypted, 0, decrypted.Length);

                                mFileReceivingNotifier.Add(decrypted.Length);
                                #endregion
                                break;

                            case PacketType.FileSendEnd:
                                #region FileSendEnd
                                if (!mFileReceiving)
                                    throw new InvalidOperationException("파일을 받는 중이 아닌 시점에 파일 전송 종료 패킷이 도착하였습니다.");

                                mFileReceiving = false;
                                OnPropertyChanged("FileTransfering");
                                FileReceivingEnd?.Invoke(this, new FileReceivingEndEventArgs(mFileReceivingInfo, mFileReceiveStream));

                                mFileReceivingNotifier = null;
                                mFileReceiveStream = null;
                                mFileReceivingInfo = null;
                                #endregion
                                break;

                            case PacketType.RequestLogin:
                                #region RequestLogin
                                // 이미 로그인한 상태에서 다시 로그인을 요청해도 예외처리하지 않음.
                                // 신분을 재확인할때 이 기능을 그대로 다시 사용하기 위함임.
                                TcpLoginInfo info = TcpLoginInfo.Deserialize(mAES.Decrypt(ReceiveCompletely(packet.DataLength)));
                                TcpLoginResult result = DatabaseManager.MatchLoginInfo(info);
                                Debug.Message($"{IP}에서 로그인을 요청하였습니다. ID : {info.ID}");

                                LoginAccepted = result.Success;

                                OnPropertyChanged("LoginAccepted");

                                if (LoginAccepted)
                                {
                                    AccountID = result.ID;
                                    AccountName = result.Name;
                                    AccountType = result.Type;

                                    OnPropertyChanged("AccountID", "AccountName", "AccountType");

                                    Debug.Message($"{IP}의 로그인이 성공하였습니다. ID : {info.ID}");
                                }
                                else
                                {
                                    Debug.Message($"{IP}의 로그인이 실패하였습니다. ID : {info.ID}");
                                }

                                byte[] resultBuffer = result.Serialize();
                                byte[] resultEncrypted = mAES.Encrypt(resultBuffer);
                                Packet reply = new Packet(PacketType.LoginResult, resultEncrypted.Length);

                                mNetStream.Write(reply.ToBytes(), 0, Packet.SIZE);
                                mNetStream.Write(resultEncrypted, 0, resultEncrypted.Length); 
                                #endregion
                                break;

                            case PacketType.LoginResult:
                                #region LoginResult
                                TcpLoginResult loginResult = TcpLoginResult.Deserialize(mAES.Decrypt(ReceiveCompletely(packet.DataLength)));
                                LoginAccepted = loginResult.Success;

                                OnPropertyChanged("LoginAccepted");

                                if (LoginAccepted)
                                {
                                    AccountID = loginResult.ID;
                                    AccountName = loginResult.Name;
                                    AccountType = loginResult.Type;

                                    OnPropertyChanged("AccountID", "AccountName", "AccountType");
                                }

                                ReceivedLoginResult?.Invoke(this, new ReceivedLoginResultEventArgs(loginResult)); 
                                #endregion
                                break;

                            case PacketType.RequestDataSet:
                                #region RequestDataSet
                                Debug.Message($"{IP}에서 데이터베이스 접근 시도.");
                                TcpDataSetRequirement requirement = TcpDataSetRequirement.Deserialize(mAES.Decrypt(ReceiveCompletely(packet.DataLength)));
                                TcpDataSetResult dataSetResult;
                                Debug.Message($"상세 SQL 인수 목록 : {{{(requirement == null ? "null" : string.Join(", ", requirement.Columns))}}}, {requirement.TableName ?? "null"}, {requirement.Where ?? "null"}");

                                DataSet dataSet = null;
                                string exMessage = null;

                                try
                                {
                                    dataSet = DatabaseManager.DBSelect(requirement);
                                }
                                catch (Exception ex)
                                {
                                    exMessage = ex.Message;
                                }

                                // 성공
                                if (exMessage == null)
                                {
                                    dataSetResult = new TcpDataSetResult(dataSet, requirement);
                                    Debug.Message("데이터베이스에서 데이터 Select 성공.");
                                }
                                // 실패
                                else
                                {
                                    dataSetResult = new TcpDataSetResult(exMessage, requirement);
                                    Debug.Message($"데이터베이스에서 데이터 Select 실패. : {exMessage}");
                                }

                                byte[] dataSetResultBuffer = dataSetResult.Serialize();
                                byte[] dataSetResultEncrypted = mAES.Encrypt(dataSetResultBuffer);
                                Packet dataSetResultPacket = new Packet(PacketType.DataSetResult, dataSetResultEncrypted.Length);

                                mNetStream.Write(dataSetResultPacket.ToBytes(), 0, Packet.SIZE);
                                mNetStream.Write(dataSetResultEncrypted, 0, dataSetResultEncrypted.Length);

                                Debug.Message($"데이터베이스에서 얻은 데이터를 {IP}에 전송하였습니다."); 
                                #endregion
                                break;

                            case PacketType.DataSetResult:
                                #region DataSetResult
                                TcpDataSetResult receivedResult = TcpDataSetResult.Deserialize(mAES.Decrypt(ReceiveCompletely(packet.DataLength)));

                                ReceivedDataSetResult?.Invoke(this, new ReceivedDataSetResultEventArgs(receivedResult)); 
                                #endregion
                                break;

                            case PacketType.DataInsert:
                                #region DataInsert
                                Debug.Message($"{IP}로부터 Insert 명령을 받았습니다.");
                                TcpDataInsert dataInsert = TcpDataInsert.Deserialize(mAES.Decrypt(ReceiveCompletely(packet.DataLength)));

                                DatabaseManager.DBInsert(dataInsert);
                                Debug.Message($"{IP}의 Insert 명령을 수행하였습니다.");

                                byte[] dataInsertBuffer = dataInsert.Serialize();
                                byte[] dataInsertEncrypted = mAES.Encrypt(dataInsertBuffer);
                                Packet dataInsertPacket = new Packet(PacketType.DataInsertCompleted, dataInsertEncrypted.Length);

                                mNetStream.Write(dataInsertPacket.ToBytes(), 0, Packet.SIZE);
                                mNetStream.Write(dataInsertEncrypted, 0, dataInsertEncrypted.Length); 
                                #endregion
                                break;

                            case PacketType.DataInsertCompleted:
                                #region DataInsertCompleted
                                TcpDataInsert dataInsertLagacy = TcpDataInsert.Deserialize(mAES.Decrypt(ReceiveCompletely(packet.DataLength)));
                                InsertedData?.Invoke(this, new InsertedDataEventArgs(dataInsertLagacy)); 
                                #endregion
                                break;

                            case PacketType.DataUpdate:
                                #region DataUpdate
                                Debug.Message($"{IP}로부터 Update 명령을 받았습니다.");
                                TcpDataUpdate dataUpdate = TcpDataUpdate.Deserialize(mAES.Decrypt(ReceiveCompletely(packet.DataLength))); 

                                DatabaseManager.DBUpdate(dataUpdate);
                                Debug.Message($"{IP}의 Update 명령을 수행하였습니다.");

                                /*byte[] dataUpdateBuffer = dataUpdate.Serialize();
                                byte[] dataUpdateEncrypted = mAES.Encrypt(dataUpdateBuffer);
                                Packet dataUpdatePacket = new Packet(PacketType.DataUpdateCompleted, dataUpdateEncrypted.Length);

                                mNetStream.Write(dataUpdatePacket.ToBytes(), 0, Packet.SIZE);
                                mNetStream.Write(dataUpdateEncrypted, 0, dataUpdateEncrypted.Length);*/
                                #endregion
                                break;

                            case PacketType.Register:
                                #region Register
                                Debug.Message($"{IP}로부터 회원가입 요청을 수신하였습니다.");
                                TcpAccountInfo accountInfo = TcpAccountInfo.Deserialize(mAES.Decrypt(ReceiveCompletely(packet.DataLength)));

                                bool regSuccess = DatabaseManager.TryToRegister(accountInfo, false);

                                if (regSuccess)
                                    Debug.Message($"회원가입 요청을 정상적으로 처리했습니다.");
                                else
                                    Debug.Message("이미 있는 ID 이므로 요청이 거부되었습니다.");

                                TcpRegisterResult regResult = new TcpRegisterResult(regSuccess, accountInfo);

                                byte[] regResultBuffer = regResult.Serialize();
                                byte[] regResultEncrypted = mAES.Encrypt(regResultBuffer);
                                Packet regResultPacket = new Packet(PacketType.RegisterResult, regResultEncrypted.Length);

                                mNetStream.Write(regResultPacket.ToBytes(), 0, Packet.SIZE);
                                mNetStream.Write(regResultEncrypted, 0, regResultEncrypted.Length); 
                                #endregion
                                break;

                            case PacketType.RegisterResult:
                                #region RegisterResult
                                TcpRegisterResult regResultResponse = TcpRegisterResult.Deserialize(mAES.Decrypt(ReceiveCompletely(packet.DataLength)));
                                ReceivedRegisterResult?.Invoke(this, new ReceivedRegisterResultEventArgs(regResultResponse)); 
                                #endregion
                                break;

                            case PacketType.DataDelete:
                                #region DataDelete
                                Debug.Message($"{IP}로부터 Delete 명령을 받았습니다.");
                                TcpDataDelete dataDelete = TcpDataDelete.Deserialize(mAES.Decrypt(ReceiveCompletely(packet.DataLength)));

                                Debug.Message($"상세 SQL 인수 목록 : {dataDelete.TableName ?? "null"}, {dataDelete.Where ?? "null"}");

                                DatabaseManager.DBDelete(dataDelete);
                                Debug.Message($"{IP}의 Delete 명령을 수행하였습니다."); 
                                #endregion
                                break;

                            case PacketType.Disconnect:
                                if (DoesSendPing)
                                    Debug.Message($"클라이언트 {IP}가 정상적으로 연결을 종료하였습니다. (Disconnect 패킷 수신함)");

                                else
                                    Debug.Error($"통신 중 문제가 발생하여 서버에서 연결을 강제로 종료하였습니다!");

                                Disconnect();
                                break;

                            case PacketType.AESIV:
                            case PacketType.AESKey:
                            case PacketType.RSAPublic:
                                throw new InvalidOperationException("초기 설정에 대한 패킷이 올바르지 않은 시간에 도착하였습니다.");

                            default:
                                throw new InvalidOperationException($"전송받은 패킷의 타입이 올바르지 않습니다. : {packet.Type.ToString()}");
                        }
                    }


                    Thread.Sleep(5);
                }
            }
            catch (Exception e)
            {
                Disconnect();
                Debug.Error($"{IP}와 통신 중 예외 발생, 연결 종료 : {e.GetType().ToString()}, {e.Message}\n\n{e.StackTrace}");
            }
        }

        private Packet ReceivePacket()
        {
            byte[] packetBuffer = ReceiveCompletely(Packet.SIZE);

            return Packet.BytesToPacket(packetBuffer);
        }

        private Packet ReceivePacket(PacketType type)
        {
            byte[] packetBuffer = ReceiveCompletely(Packet.SIZE);

            Packet received = Packet.BytesToPacket(packetBuffer);

            if (received.Type == type)
                return received;

            else throw new InvalidOperationException($"예상한 패킷이 도달하지 않았습니다. 예상 : {type.ToString()}");
        }

        // 서버만 이 메서드 실행
        private void SendPing()
        {
            //Debug.Message($"{IP}에 핑을 먼저 보냄...");

            try
            {
                byte[] buffer = Packet.PingPacket.ToBytes();
                mNetStream.Write(buffer, 0, buffer.Length);

                mLastPingSend = DateTime.Now;
            }
            catch (Exception)
            {
                throw new IOException($"핑을 {IP}의 NetworkStream에 쓰는 중 예외가 발생하였습니다. 연결이 끊긴 것으로 보입니다.");
            }
        }

        private void HandleMessage(int bytesLength)
        {
            byte[] buffer = ReceiveCompletely(bytesLength);

            string message = Encoding.UTF8.GetString(mAES.Decrypt(buffer));
            ReceivedMessage?.Invoke(this, new TcpCore.ReceivedMessageEventArgs(message));
        }

        private void HandlePing()
        {
            //Debug.Message("HandlePing 메서드 진입.");

            // DoesSendPing이 true라면 이 객체는 서버에서 생성한 것이고, 이때 받은 핑은 클라이언트가 서버로 보내는 응답 핑임. 
            if (DoesSendPing)
            {
                //Debug.Message($"{IP}로 부터 응답 핑을 받았음.");
                mLastPingReceive = DateTime.Now;
            }
            else
            {
                byte[] buffer = Packet.PingPacket.ToBytes();
                mNetStream.Write(buffer, 0, buffer.Length);

                //Debug.Message($"{IP}의 핑에 대한 응답을 하였음.");
            }
        }

        public void SendMessage(string message)
        {
            byte[] buffer = mAES.Encrypt(Encoding.UTF8.GetBytes(message));
            Packet p = new Packet(PacketType.Message, buffer.Length);
            mNetStream.Write(p.ToBytes(), 0, Packet.SIZE);
            mNetStream.Write(buffer, 0, buffer.Length);
        }

        public TcpTransferNotifier StartSendingFile(string path)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (!File.Exists(path))
                throw new ArgumentException("파일을 전송하는데 지정된 경로가 존재하지 않습니다.");

            if (mFileSending)
                throw new InvalidOperationException("파일이 이미 전송중인 중에 또다른 파일 전송을 요청하였습니다.");

            mFileSending = true;

            FileInfo fileInfo = new FileInfo(path);
            TcpFileInfo tcpFileInfo = new TcpFileInfo(fileInfo.Name, fileInfo.Length);
            byte[] infoBuffer = tcpFileInfo.Serialize();

            Packet packet = new Packet(PacketType.FileSendStart, infoBuffer.Length);
            mNetStream.Write(packet.ToBytes(), 0, Packet.SIZE);
            mNetStream.Write(infoBuffer, 0, infoBuffer.Length);

            mFileSendingNotifier = new TcpTransferNotifier(tcpFileInfo.Length);

            ThreadPool.QueueUserWorkItem((o) => 
            {
                FileStream fs = null;

                try
                {
                    Packet sendingPacket = new Packet(PacketType.FileSending, 0);
                    byte[] buffer = new byte[FILE_TRANS_SIZE];
                    byte[] encrypted;
                    int read = 0;

                    fs = new FileStream(path, FileMode.Open);

                    while (fs.Position != fs.Length)
                    {
                        read = fs.Read(buffer, 0, buffer.Length);
                        encrypted = mAES.Encrypt(buffer, 0, read);

                        sendingPacket.DataLength = encrypted.Length;

                        mNetStream.Write(sendingPacket.ToBytes(), 0, Packet.SIZE);
                        mNetStream.Write(encrypted, 0, encrypted.Length);

                        mFileSendingNotifier.Add(read);
                    }

                    mNetStream.Write(new Packet(PacketType.FileSendEnd, 0).ToBytes(), 0, Packet.SIZE);
                }
                catch (Exception e)
                {
                    Debug.Error($"파일을 {IP}에 전송하는 중 예외가 발생하였습니다. 연결을 종료합니다. : {e.Message}");
                    Disconnect();
                }
                finally
                {
                    fs?.Close();
                    mFileSending = false;
                    mFileSendingNotifier = null;
                }
            });

            return mFileSendingNotifier;
        }

        public void RequestLogin(string id, string pw)
        {
            // 이미 로그인한 상태에서 다시 로그인을 요청해도 예외처리하지 않음.
            // 신분을 재확인할때 이 기능을 그대로 다시 사용하기 위함임.

            if (DoesSendPing)
                throw new InvalidOperationException("서버는 로그인을 요청할 수 없습니다.");

            TcpLoginInfo loginInfo = new TcpLoginInfo(id, pw);
            byte[] buffer = loginInfo.Serialize();
            byte[] encrypted = mAES.Encrypt(buffer);
            Packet packet = new Packet(PacketType.RequestLogin, encrypted.Length);

            mNetStream.Write(packet.ToBytes(), 0, Packet.SIZE);
            mNetStream.Write(encrypted, 0, encrypted.Length);
        }

        public void RequestDataSet(TcpDataSetRequirement requirement)
        {
            if (requirement == null)
                throw new ArgumentNullException("requirement");

            byte[] buffer = requirement.Serialize();
            byte[] encrypted = mAES.Encrypt(buffer);
            Packet packet = new Packet(PacketType.RequestDataSet, encrypted.Length);

            mNetStream.Write(packet.ToBytes(), 0, Packet.SIZE);
            mNetStream.Write(encrypted, 0, encrypted.Length);
        }

        public void InsertData(TcpDataInsert insert)
        {
            if (insert == null)
            {
                throw new ArgumentNullException(nameof(insert));
            }

            byte[] buffer = insert.Serialize();
            byte[] encrypted = mAES.Encrypt(buffer);
            Packet packet = new Packet(PacketType.DataInsert, encrypted.Length);

            mNetStream.Write(packet.ToBytes(), 0, Packet.SIZE);
            mNetStream.Write(encrypted, 0, encrypted.Length);
        }

        public void UpdateData(TcpDataUpdate update)
        {
            if (update == null)
            {
                throw new ArgumentNullException(nameof(update));
            }

            byte[] buffer = update.Serialize();
            byte[] encrypted = mAES.Encrypt(buffer);
            Packet packet = new Packet(PacketType.DataUpdate, encrypted.Length);

            mNetStream.Write(packet.ToBytes(), 0, Packet.SIZE);
            mNetStream.Write(encrypted, 0, encrypted.Length);
        }

        public void DeleteData(TcpDataDelete delete)
        {
            if (delete == null)
            {
                throw new ArgumentNullException(nameof(delete));
            }

            byte[] buffer = delete.Serialize();
            byte[] encrypted = mAES.Encrypt(buffer);
            Packet packet = new Packet(PacketType.DataDelete, encrypted.Length);

            mNetStream.Write(packet.ToBytes(), 0, Packet.SIZE);
            mNetStream.Write(encrypted, 0, encrypted.Length);
        }

        public void Register(TcpAccountInfo accountInfo)
        {
            if (accountInfo == null)
            {
                throw new ArgumentNullException(nameof(accountInfo));
            }

            byte[] buffer = accountInfo.Serialize();
            byte[] encrypted = mAES.Encrypt(buffer);
            Packet packet = new Packet(PacketType.Register, encrypted.Length);

            mNetStream.Write(packet.ToBytes(), 0, Packet.SIZE);
            mNetStream.Write(encrypted, 0, encrypted.Length);
        }

        public void Disconnect()
        {
            if (IsOnline)
            {
                try
                {
                    if (mLowClient.Connected)
                        mNetStream.Write(new Packet(PacketType.Disconnect).ToBytes(), 0, Packet.SIZE);
                }
                catch (Exception) { }

                IsOnline = false;
                mRSA?.Dispose();
                mAES?.Dispose();

                mLowClient?.Close();
                mNetStream?.Close();

                DisconnectedTime = DateTime.Now;
                Debug.Message($"EventClient가 연결 종료됨 : {DisconnectedTime.Value}, {IP}");
                Disconnected?.Invoke(this, new DisconnectedEventArgs(DisconnectionType.General));

                OnPropertyChanged("IsOnline", "DisconnectedTime");
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
                    if (IsOnline)
                        Disconnect();
                }

                // TODO: 관리되지 않는 리소스(관리되지 않는 개체)를 해제하고 아래의 종료자를 재정의합니다.
                // TODO: 큰 필드를 null로 설정합니다.

                disposedValue = true;
            }
        }

        ~EventClient()
        {
            // 이 코드를 변경하지 마세요. 위의 Dispose(bool disposing)에 정리 코드를 입력하세요.
            Dispose(false);
        }

        // 삭제 가능한 패턴을 올바르게 구현하기 위해 추가된 코드입니다.
        public void Dispose()
        {
            // 이 코드를 변경하지 마세요. 위의 Dispose(bool disposing)에 정리 코드를 입력하세요.
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        private void OnPropertyChanged(params string[] names)
        {
            if (names == null) return;

            foreach (string name in names)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }
    }

    public enum DisconnectionType { Timeout, General }

    public class DisconnectedEventArgs : EventArgs
    {
        public DisconnectionType Type { get; private set; }

        public DisconnectedEventArgs(DisconnectionType dType)
        {
            Type = dType;
        }
    }

    public class ReceivedMessageEventArgs : EventArgs
    {
        public string Message { get; private set; }

        public ReceivedMessageEventArgs(string message)
        {
            Message = message;
        }
    }

    public class FileReceivingStartedEventArgs : EventArgs
    {
        public TcpFileInfo Info { get; }
        public Stream Stream { get; set; } = null;
        public TcpTransferNotifier Notifier { get; }

        public FileReceivingStartedEventArgs(TcpFileInfo info, TcpTransferNotifier notifier)
        {
            Info = info.Clone() as TcpFileInfo;
            Notifier = notifier;
        }
    }

    public class FileReceivingEndEventArgs : EventArgs
    {
        public TcpFileInfo Info { get; }
        public Stream Stream { get; }

        public FileReceivingEndEventArgs(TcpFileInfo info, Stream stream)
        {
            Info = info;
            Stream = stream;
        }
    }

    public class ReceivedLoginResultEventArgs : EventArgs
    {
        public TcpLoginResult Result { get; }

        public ReceivedLoginResultEventArgs(TcpLoginResult result)
        {
            Result = result ?? throw new ArgumentNullException("result");
        }
    }

    public class ReceivedDataSetResultEventArgs : EventArgs
    {
        public TcpDataSetResult Result { get; }

        public ReceivedDataSetResultEventArgs(TcpDataSetResult result)
        {
            Result = result ?? throw new ArgumentNullException("result");
        }
    }

    public class InsertedDataEventArgs : EventArgs
    {
        public TcpDataInsert Insert { get; }

        public InsertedDataEventArgs(TcpDataInsert insert)
        {
            Insert = insert ?? throw new ArgumentNullException("insert");
        }
    }

    public class ReceivedRegisterResultEventArgs : EventArgs
    {
        public TcpRegisterResult Result { get; }

        public ReceivedRegisterResultEventArgs(TcpRegisterResult result)
        {
            Result = result ?? throw new ArgumentNullException("result");
        }
    }
}
