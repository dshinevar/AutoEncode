using AutomatedFFmpegUtilities;
using AutomatedFFmpegUtilities.Enums;
using AutomatedFFmpegUtilities.Messages;
using AutomatedFFmpegUtilities.Messages.ClientToServer;
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
        private const int BUFFER_SIZE = 1024;
        private byte[] _buffer = new byte[BUFFER_SIZE];
        private IPAddress _serverIP;
        private IPEndPoint _endPoint;
        private Socket _serverSocketListener;
        private Socket _clientHandler;
        private ManualResetEvent _disconnectDone = new ManualResetEvent(false);
        private JsonSerializerSettings _settings = new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.All
        };
        private AFServerMainThread _mainThreadHandle;

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
        public AFServerSocket(AFServerMainThread thread, string IP, int port)
        {
            _mainThreadHandle = thread;
            _serverIP = IPAddress.Parse(IP);
            _endPoint = new IPEndPoint(_serverIP, port);
        }

        public void Dispose()
        {
            if (IsConnected()) Disconnect(false);

            _clientHandler.Close();
            _serverSocketListener.Dispose();
        }

        #region CONNECT
        public bool IsConnected() => !(((_clientHandler?.Poll(1000, SelectMode.SelectRead) ?? false) && (_clientHandler?.Available == 0)) || !(_clientHandler?.Connected ?? false));

        public void StartListening()
        {
            try
            {
                if (_serverSocketListener == null) _serverSocketListener = new Socket(_serverIP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                if (!_serverSocketListener.IsBound) _serverSocketListener.Bind(_endPoint);
                _serverSocketListener.Listen(1);

                Debug.WriteLine("Waiting for connection...");
                _serverSocketListener.BeginAccept(new AsyncCallback(AcceptCallback), _serverSocketListener);

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            Socket listener = (Socket)ar.AsyncState;
            _clientHandler = listener.EndAccept(ar);

            Debug.WriteLine("Connected to Client");

            StateObject state = new StateObject();
            state.clientSocket = _clientHandler;

            _mainThreadHandle.AddSendClientConnectData();
            _clientHandler.BeginReceive(_buffer, 0, BUFFER_SIZE, 0, new AsyncCallback(ReadCallback), state);
        }
        #endregion CONNECT

        #region DISCONNECT
        public void Disconnect(bool restartListener = true)
        {
            try
            {
                _clientHandler.Shutdown(SocketShutdown.Both);
                _clientHandler.BeginDisconnect(true, new AsyncCallback(DisconnectCallback), _clientHandler);
                _disconnectDone.WaitOne();
                _disconnectDone.Reset();
            }
            catch { }

            if (restartListener) StartListening();
        }

        private void DisconnectCallback(IAsyncResult ar)
        {
            Socket handler = (Socket)ar.AsyncState;
            handler.EndDisconnect(ar);
            _disconnectDone.Set();
        }
        #endregion DISCONNECT

        #region SEND/RECEIVE
        public void Send(AFMessageBase msg)
        {
            if (!IsConnected()) return;
            byte[] byteData = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(msg, _settings));

            // Begin sending the data to the remote device.  
            _clientHandler.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), _clientHandler);
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
                    state.stringBuffer.Append(Encoding.ASCII.GetString(_buffer, 0, bytesRead));

                    if (state.stringBuffer.ToString().IsValidJson())
                    {
                        object msg = JsonConvert.DeserializeObject<AFMessageBase>(state.stringBuffer.ToString(), _settings);
                        if (msg is CTSTest)
                        {
                            Debug.WriteLine($"Message from client: {((AFMessageBase)msg).MessageType}");
                            if (((CTSTest)msg).MessageType.Equals(AFMessageType.DISCONNECT))
                            {
                                Disconnect();
                                return;
                            }
                            else
                            {
                                _mainThreadHandle.AddProcessMessage((AFMessageBase)msg);
                            }
                        }
                        state.stringBuffer.Clear();
                        handler.BeginReceive(_buffer, 0, BUFFER_SIZE, 0, new AsyncCallback(ReadCallback), state);
                    }
                    else
                    {
                        // Not all data received. Get more. 
                        handler.BeginReceive(_buffer, 0, BUFFER_SIZE, 0, new AsyncCallback(ReadCallback), state);
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
