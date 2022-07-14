using AutomatedFFmpegUtilities;
using AutomatedFFmpegUtilities.Enums;
using AutomatedFFmpegUtilities.Messages;
using AutomatedFFmpegUtilities.Logger;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace AutomatedFFmpegServer.ServerSocket
{
    public class AFServerSocket : IDisposable
    {
        private const int BUFFER_SIZE = 4096;
        private byte[] Buffer = new byte[BUFFER_SIZE];
        private IPAddress ServerIP;
        private IPEndPoint EndPoint;
        private Socket Listener = null;
        private Socket ClientHandler = null;
        private ManualResetEvent DisconnectDone = new ManualResetEvent(false);
        private JsonSerializerSettings JsonSettings = new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.All
        };
        private AFServerMainThread MainThreadHandle = null;
        private Logger Logger { get; set; }


        // State object for reading client data asynchronously  
        private class StateObject
        {
            // Received data string.
            public StringBuilder stringBuffer = new StringBuilder();

            // Client socket.
            public Socket clientSocket = null;
        }

        /// <summary> Constructor </summary>
        /// <param name="IP">IP Address of the Server</param>
        /// <param name="port">Port to bind to.</param>
        public AFServerSocket(AFServerMainThread thread, Logger logger, string IP, int port)
        {
            MainThreadHandle = thread;
            Logger = logger;
            ServerIP = IPAddress.Parse(IP);
            EndPoint = new IPEndPoint(ServerIP, port);
        }

        public void Dispose()
        {
            if (IsConnected()) Disconnect(false);

            ClientHandler.Close();
            Listener.Dispose();
        }

        #region CONNECT
        public bool IsConnected() => !(((ClientHandler?.Poll(1000, SelectMode.SelectRead) ?? false) && (ClientHandler?.Available == 0)) || !(ClientHandler?.Connected ?? false));

        public void StartListening()
        {
            try
            {
                if (Listener is null) Listener = new Socket(ServerIP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                if (!Listener.IsBound) Listener.Bind(EndPoint);
                Listener.Listen(1);

                Debug.WriteLine("Waiting for connection...");
                Listener.BeginAccept(new AsyncCallback(AcceptCallback), Listener);

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            Socket listener = (Socket)ar.AsyncState;
            ClientHandler = listener.EndAccept(ar);

            Debug.WriteLine("Connected to Client");

            StateObject state = new StateObject();
            state.clientSocket = ClientHandler;

            MainThreadHandle.AddSendClientConnectData();
            ClientHandler.BeginReceive(Buffer, 0, BUFFER_SIZE, 0, new AsyncCallback(ReadCallback), state);
        }
        #endregion CONNECT

        #region DISCONNECT
        public void Disconnect(bool restartListener = true)
        {
            try
            {
                ClientHandler.Shutdown(SocketShutdown.Both);
                ClientHandler.BeginDisconnect(true, new AsyncCallback(DisconnectCallback), ClientHandler);
                DisconnectDone.WaitOne();
                DisconnectDone.Reset();
            }
            catch { }

            if (restartListener) StartListening();
        }

        private void DisconnectCallback(IAsyncResult ar)
        {
            Socket handler = (Socket)ar.AsyncState;
            handler.EndDisconnect(ar);
            DisconnectDone.Set();
        }
        #endregion DISCONNECT

        #region SEND/RECEIVE
        public void Send(AFMessageBase msg)
        {
            if (!IsConnected()) return;
            byte[] byteData = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(msg, JsonSettings));

            // Begin sending the data to the remote device.  
            ClientHandler.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), ClientHandler);
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket handler = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.  
                int bytesSent = handler.EndSend(ar);
                Debug.WriteLine($"Sent {bytesSent} bytes to client.");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }
        }

        private void ReadCallback(IAsyncResult ar)
        {
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.clientSocket;

            try
            {
                int bytesRead = handler.EndReceive(ar);

                if (bytesRead > 0)
                {
                    // There  might be more data, so store the data received so far.  
                    state.stringBuffer.Append(Encoding.ASCII.GetString(Buffer, 0, bytesRead));

                    if (state.stringBuffer.ToString().IsValidJson())
                    {
                        object msg = JsonConvert.DeserializeObject<AFMessageBase>(state.stringBuffer.ToString(), JsonSettings);
                        if (msg is AFMessageBase)
                        {
                            Debug.WriteLine($"Message from client: {((AFMessageBase)msg).MessageType}");
                            if (((AFMessageBase)msg).MessageType.Equals(AFMessageType.DISCONNECT))
                            {
                                Disconnect();
                                return;
                            }
                            else
                            {
                                MainThreadHandle.AddProcessMessage((AFMessageBase)msg);
                            }
                        }
                        state.stringBuffer.Clear();
                        handler.BeginReceive(Buffer, 0, BUFFER_SIZE, 0, new AsyncCallback(ReadCallback), state);
                    }
                    else
                    {
                        // Not all data received. Get more. 
                        handler.BeginReceive(Buffer, 0, BUFFER_SIZE, 0, new AsyncCallback(ReadCallback), state);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                Disconnect();
            }
        }
        #endregion SEND/RECEIVE
    }
}
