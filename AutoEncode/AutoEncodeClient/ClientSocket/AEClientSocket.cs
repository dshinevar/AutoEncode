using AutoEncodeUtilities;
using AutoEncodeUtilities.Enums;
using AutoEncodeUtilities.Messages;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace AutoEncodeClient.ClientSocket
{
    public class AEClientSocket
    {
        private const int BUFFER_SIZE = 4096;
        private byte[] _buffer = new byte[BUFFER_SIZE];

        private IPAddress _serverIP { get; set; }
        private IPEndPoint _endPoint { get; set; }
        private Socket _clientSocket { get; set; }
        private ManualResetEvent _connectDone = new ManualResetEvent(false);
        private ManualResetEvent _disconnectDone = new ManualResetEvent(false);
        private JsonSerializerSettings _serializerSettings = new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.All
        };

        private AEClientMainThread _mainThreadHandle { get; set; }

        // State object for receiving data from remote device.  
        public class StateObject
        {
            // Client socket.  
            public Socket stateSocket = null;
            // Received data string.  
            public StringBuilder stringBuffer = new StringBuilder();
        }

        public AEClientSocket(AEClientMainThread thread, string ipAddress, int port)
        {
            _mainThreadHandle = thread;
            _serverIP = IPAddress.Parse(ipAddress);
            _endPoint = new IPEndPoint(_serverIP, port);
        }

        public void Close()
        {
            Disconnect();
            _clientSocket.Close();
        }

        #region CONNECT
        public bool IsConnected()
        {
            try
            {
                return !(((_clientSocket?.Poll(1000, SelectMode.SelectRead) ?? false) && (_clientSocket?.Available == 0)) || !(_clientSocket?.Connected ?? false));
            }
            catch (ObjectDisposedException)
            {
                // Mainly used in case something outside this class tries calling IsConnected() before shutdown.
                return false;
            }
        }
        public bool Connect()
        {
            if (IsConnected()) return true;
            try
            {
                _clientSocket = new Socket(_serverIP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                _clientSocket.BeginConnect(_endPoint, new AsyncCallback(ConnectCallback), _clientSocket);
                _connectDone.WaitOne();
                bool connected = IsConnected();
                _disconnectDone.Reset();

                if (connected) StartReceiving();

                return connected;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception: {ex}");
                return false;
            }
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            Socket client = (Socket)ar.AsyncState;
            try
            {
                client.EndConnect(ar);

                Debug.WriteLine($"Socket connected to {_clientSocket.RemoteEndPoint}");

                _connectDone.Set();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                _connectDone.Set();
            }
        }
        #endregion CONNECT

        #region DISCONNECT
        public void Disconnect()
        {
            if (!IsConnected()) return;
            Send(new AEMessageBase() { MessageType = AEMessageType.DISCONNECT });
            _clientSocket.Shutdown(SocketShutdown.Both);
            _clientSocket.BeginDisconnect(true, new AsyncCallback(DisconnectCallback), _clientSocket);

            _disconnectDone.WaitOne();
            _connectDone.Reset();
        }

        private void DisconnectCallback(IAsyncResult ar)
        {
            Socket client = (Socket)ar.AsyncState;
            client.EndDisconnect(ar);
            _disconnectDone.Set();
        }
        #endregion DISCONNECT

        #region SEND/RECEIVE
        private void StartReceiving()
        {
            try
            {
                // Create the state object.  
                StateObject state = new StateObject();
                state.stateSocket = _clientSocket;

                // Begin receiving the data from the remote device.  
                _clientSocket.BeginReceive(_buffer, 0, BUFFER_SIZE, 0, new AsyncCallback(ReceiveCallback), state);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the state object and the client socket
                // from the asynchronous state object.  
                StateObject state = (StateObject)ar.AsyncState;
                Socket client = state.stateSocket;

                // Read data from the remote device.  
                int bytesRead = client.EndReceive(ar);

                if (bytesRead > 0)
                {
                    state.stringBuffer.Append(Encoding.ASCII.GetString(_buffer, 0, bytesRead));

                    if (state.stringBuffer.ToString().IsValidJson())
                    {
                        object msg = JsonConvert.DeserializeObject<AEMessageBase>(state.stringBuffer.ToString(), _serializerSettings);
                        if (msg is AEMessageBase)
                        {
                            //_mainThreadHandle.AddProcessMessage((AEMessageBase)msg);
                        }
                        state.stringBuffer.Clear();
                        client.BeginReceive(_buffer, 0, BUFFER_SIZE, 0, new AsyncCallback(ReceiveCallback), state);
                    }
                    else
                    {
                        // There might be more data, so store the data received so far.
                        client.BeginReceive(_buffer, 0, BUFFER_SIZE, 0, new AsyncCallback(ReceiveCallback), state);
                    }
                }
                else
                {
                    client.BeginReceive(_buffer, 0, BUFFER_SIZE, 0, new AsyncCallback(ReceiveCallback), state);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }
        }

        public void Send(AEMessageBase msg)
        {
            if (IsConnected())
            {
                // Convert the string data to byte data using ASCII encoding.  
                byte[] byteData = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(msg, _serializerSettings));

                // Begin sending the data to the remote device.  
                _clientSocket.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), _clientSocket);
            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket client = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.  
                int bytesSent = client.EndSend(ar);

                Debug.WriteLine("Sent {0} bytes to server.", bytesSent);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }
        }

        #endregion SEND/RECEIVE
    }
}
