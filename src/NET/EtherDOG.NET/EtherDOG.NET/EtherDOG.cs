using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace EtherDOGNET
{
    public struct ReceivedData
    {
        public int RequestId;
        public bool CanReceive;
        public byte[] Data;
    }

    public class EtherDOG : IDisposable
    {
        private const int MESSAGE_HEADER_LENGTH = 14;

        private readonly IPAddress _listenerIpAddress;
        private readonly string _listenerIp;
        private readonly int _listenerPort;

        private TcpListener _listener;
        private bool _isListening;

        private Task _handleMessages;

        private CancellationTokenSource _tokenSource = new CancellationTokenSource();
        private CancellationToken _token;

        public event Action<string> OnLog;
        public event Action<bool> OnStatusChanged;
        public event Action<byte[]> OnReceivedData;
        public event Func<byte[]> OnSendData;

        public EtherDOG(string ip, int port)
        {
            if (port < 0) throw new ArgumentException("Port must be zero or greater.");

            if (string.IsNullOrEmpty(ip))
            {
                _listenerIpAddress = IPAddress.Any;
            }
            else if (ip.Equals("localhost") || ip.Equals("127.0.0.1") ||
                ip.Equals("::1"))
            {
                _listenerIpAddress = IPAddress.Loopback;
            }
            else
            {
                _listenerIpAddress = IPAddress.Parse(ip);
            }

            _listenerIp = _listenerIpAddress.ToString();
            _listenerPort = port;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Start()
        {
            if (_isListening) throw new InvalidOperationException("The server is already running.");

            _listener = new TcpListener(_listenerIpAddress, _listenerPort);
            _listener.Start();
            _isListening = true;

            _token = _tokenSource.Token;

            _handleMessages = Task.Run(() => HandleMessages(), _token);
        }

        public void Stop()
        {
            if (!_isListening) throw new InvalidOperationException("The server is not running.");

            _listener.Stop();
            _tokenSource.Cancel();
            _handleMessages.Wait();
            _handleMessages = null;
        }

        private async Task HandleMessages()
        {
            while (!_token.IsCancellationRequested)
            {
                TcpClient client = null;

                try
                {
                    client = await _listener.AcceptTcpClientAsync().ConfigureAwait(false);

                    OnStatusChanged?.Invoke(true);

                    var stream = client.GetStream();

                    byte[] tmpData = new byte[0];

                    while (client.Connected)
                    {
                        if (_token.IsCancellationRequested) break;

                        // Receive the data
                        var receivedData = await ReceiveDataAsync(stream, _token);

                        // Check if the two arrays are the same
                        if (!Enumerable.SequenceEqual(tmpData, receivedData.Data))
                        {
                            // If so then raise an event
                            OnReceivedData?.Invoke(receivedData.Data);

                            // Store the new data
                            tmpData = receivedData.Data;
                        }

                        // Check if we have to send something
                        if (receivedData.CanReceive)
                        {
                            // Invoke the external action
                            byte[] dataToSend = OnSendData?.Invoke();

                            // Send the data
                            await SendDataAsync(stream, receivedData.RequestId, dataToSend, _token);
                        }
                    }
                }
                catch (Exception)
                {
                    client?.Dispose();
                    OnStatusChanged?.Invoke(false);
                }
            }
        }

        private async Task<ReceivedData> ReceiveDataAsync(NetworkStream stream, CancellationToken token)
        {
            byte[] header = new byte[MESSAGE_HEADER_LENGTH];

            // Receive the header
            int headerCount = await stream.ReadAsync(header, 0, header.Length, token);

            if (headerCount == 0)
            {
                throw new Exception("The client got disconnected while waiting for the header.");
            }

            if (header.Length < MESSAGE_HEADER_LENGTH)
            {
                throw new Exception("Header length was invalid.");
            }

            // Check the header identifier
            if (header[0] != 0x45 &&
                header[1] != 0x54 &&
                header[2] != 0x48 &&
                header[3] != 0x45 &&
                header[4] != 0x52 &&
                header[5] != 0x44 &&
                header[6] != 0x4F &&
                header[7] != 0x47)
            {
                throw new Exception("Header identifier wasn't correct.");
            }

            // Request Id
            int requestId = header[8];

            // Can receive
            bool canReceive = Convert.ToBoolean(header[9]);

            // Message length
            int messageDataLength =
                header[10] << 24 |
                header[11] << 16 |
                header[12] << 8 |
                header[13];

            // Consider it as an unsigned short
            if (messageDataLength < ushort.MinValue || messageDataLength > ushort.MaxValue)
            {
                throw new Exception("The message data length is outside the ushort ranges.");
            }

            byte[] data = new byte[messageDataLength];

            if (messageDataLength != 0)
            {
                // Read the remaining data
                int dataCount = await stream.ReadAsync(data, 0, data.Length, token);

                if (dataCount == 0)
                {
                    throw new Exception("The client got disconnected while waiting for the data or the data that it sent was zero");
                }

                if (dataCount != messageDataLength)
                {
                    throw new Exception("The received data length doesn't match the declared one");
                }
            }

            return new ReceivedData
            {
                RequestId = requestId,
                CanReceive = canReceive,
                Data = data
            };
        }

        private async Task SendDataAsync(NetworkStream stream, int requestId, byte[] data, CancellationToken token)
        {
            if (data == null)
                data = new byte[0];

            var message = new List<byte>
            {
                // Message Identifier
                0x45, // E
                0x54, // T
                0x48, // H
                0x45, // E
                0x52, // R
                0x44, // D
                0x4F, // O
                0x47, // G

                // Request Id
                Convert.ToByte(requestId),

                // Can Receive has to be up all the time when we send the message
                0x1,

                // Data length (reversed)
                (byte)(data.Length >> 24),
                (byte)(data.Length >> 16),
                (byte)(data.Length >> 8),
                (byte)(data.Length >> 0),
            };

            // Data
            message.AddRange(data);

            // Send the message
            await stream.WriteAsync(message.ToArray(), 0, message.Count, token);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    if (_tokenSource != null)
                    {
                        if (!_tokenSource.IsCancellationRequested)
                        {
                            _tokenSource.Cancel();
                        }

                        _tokenSource.Dispose();
                    }

                    if (_listener != null && _listener.Server != null)
                    {
                        _listener.Server.Close();
                        _listener.Server.Dispose();
                    }

                    _listener?.Stop();
                }
                catch (Exception)
                {
                    //
                }

                _isListening = false;
            }
        }
    }
}
