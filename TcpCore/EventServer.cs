using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.ObjectModel;

namespace TcpCore
{
    public class EventServer
    {
        private TcpListener mListener;

        public event EventHandler<ClientConnectedEventArgs> ClientConnected;
        public bool Started { get; private set; } = false;
        public bool Halted { get; private set; } = false;
        public int Port { get; }
        public MTObservableCollection<EventClient> Clients { get; private set; }

        public EventServer(int port)
        {
            Port = port;

            mListener = new TcpListener(IPAddress.Any, port);
            Clients = new MTObservableCollection<EventClient>();
        }

        public void Start()
        {
            if (!Started)
            {
                Started = true;

                mListener.Start();
                Debug.Message("지정된 포트에서 Listening 시작됨.");

                ThreadPool.QueueUserWorkItem(ListenLoop);
            }
            else
            {
                throw new InvalidOperationException("EventServer가 이미 시작되었습니다.");
            }

        }

        private void ListenLoop(object o)
        {
            try
            {
                Debug.Message("EventServer의 배경쓰레드에서 반복문 진입.");
                while (!Halted)
                {
                    TcpClient client = mListener.AcceptTcpClient();
                    EventClient eventClient = new EventClient(client, true);

                    if (eventClient == null ? false : eventClient.IsOnline)
                    {
                        eventClient.Disconnected += HandleClientDisconnection;
                        Clients.Add(eventClient);
                        ClientConnected?.Invoke(this, new ClientConnectedEventArgs(eventClient));
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Error($"EventServer의 배경쓰레드에서 예외 발생 : {e.Message}\n\n{e.StackTrace}");
                Halt();
            }
            finally
            {
                Debug.Message($"EventServer의 배경쓰레드에서 finally절 진입함.");
                // Halt 되었으므로 쓰레드가 정리됨.  
            }
        }

        private void HandleClientDisconnection(object sender, DisconnectedEventArgs e)
        {
            EventClient client = sender as EventClient;
            if (!Clients.Remove(client))
                throw new InvalidOperationException("연결이 끊긴 클라이언트를 컬렉션에서 제거하는데 실패하였습니다.");
        }

        public void Halt()
        {
            if (!Halted)
            {
                Halted = true;

                foreach (EventClient client in Clients.ToArray())
                {
                    client.Disconnect();
                }
            }
            else
            {
                return;
            }
        }
    }

    public class ClientConnectedEventArgs : EventArgs
    {
        public EventClient Client { get; private set; }

        public ClientConnectedEventArgs(EventClient client)
        {
            Client = client;
        }
    }
}
