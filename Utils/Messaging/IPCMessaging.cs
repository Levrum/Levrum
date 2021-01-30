using System;
using System.Collections.Generic;
using System.Text;

using ServiceWire;
using ServiceWire.NamedPipes;

namespace Levrum.Utils.Messaging
{
    public enum IPCMessageType { Empty, OpenDocument, BringToFront, CloseApp }

    public interface IMessageService
    {
        Guid SendMessage(IPCMessage message);
        IPCMessage GetMessage(Guid messageId);
        void SendResponse(IPCMessage message);
        IPCMessage GetResponse(Guid messageId);

        event IPCMessageDelegate OnMessageReceived;
    }

    [Serializable]
    public class IPCMessage
    {
        public Guid Id { get; set; } = Guid.Empty;
        public Guid ResponseId { get; set; } = Guid.Empty;
        public Guid Source { get; set; } = Guid.Empty;
        public Guid Destination { get; set; } = Guid.Empty;
        public IPCMessageType Type { get; set; } = IPCMessageType.Empty;
        public object Data { get; set; } = null;

        public IPCMessage()
        {
            Id = Guid.NewGuid();
        }

        public IPCMessage(Guid _id, Guid _responseId, Guid _source, Guid _destination, IPCMessageType _type, object _data)
        {
            Id = _id;
            ResponseId = _responseId;
            Source = _source;
            Destination = _destination;
            Type = _type;
            Data = _data;
        }

        public static IPCMessage Empty { get; } = new IPCMessage() { Id = Guid.Empty };
    }

    public delegate void IPCMessageDelegate(IPCMessage message);

    public class IPCService : IMessageService
    {
        public Dictionary<Guid, IPCMessage> MessageLog { get; set; } = new Dictionary<Guid, IPCMessage>();
        public Dictionary<Guid, IPCMessage> ResponseLog { get; set; } = new Dictionary<Guid, IPCMessage>();
        public Dictionary<Guid, IPCMessage> ResponseQueue { get; set; } = new Dictionary<Guid, IPCMessage>();

        public event IPCMessageDelegate OnMessageReceived;

        /// <summary>
        /// Used by clients to send messages
        /// </summary>
        /// <param name="message"></param>
        public Guid SendMessage(IPCMessage message)
        {
            if (MessageLog.ContainsKey(message.Id))
            {
                // Repeated message
                IPCMessage lastMessage = MessageLog[message.Id];
                return lastMessage.ResponseId;
            }

            message.ResponseId = Guid.NewGuid();
            MessageLog.Add(message.Id, message);

            OnMessageReceived?.Invoke(message);

            return message.ResponseId;
        }

        public IPCMessage GetMessage(Guid messageId)
        {
            if (!MessageLog.ContainsKey(messageId))
            {
                return null;
            }

            return MessageLog[messageId];
        }
             
        public void SendResponse(IPCMessage message)
        {
            lock (ResponseQueue)
                lock (ResponseLog)
                {
                    if (ResponseQueue.ContainsKey(message.Id) || ResponseLog.ContainsKey(message.Id))
                    {
                        // Repeated response
                        return;
                    }

                    ResponseQueue.Add(message.Id, message);
                }
        }

        public IPCMessage GetResponse(Guid responseId)
        {
            lock (ResponseQueue)
                lock (ResponseLog)
                {
                    if (ResponseQueue.ContainsKey(responseId))
                    {
                        IPCMessage response = ResponseQueue[responseId];
                        ResponseQueue.Remove(responseId);
                        ResponseLog.Add(responseId, response);
                        return response;
                    } else if (ResponseLog.ContainsKey(responseId))
                    {
                        IPCMessage response = ResponseLog[responseId];
                        return response;
                    } else
                    {
                        return null;
                    }
                }
        }
    }

    public class IPCNamedPipeServer : IMessageService, IDisposable
    {
        public string PipeName { get; protected set; }
        public NpHost NpHost { get; set; }
        public IMessageService Service { get; set; }

        public Logger Logger { get; set; }
        public Stats Stats { get; set; }

        public event IPCMessageDelegate OnMessageReceived;

        public IPCNamedPipeServer (string _pipeName, IMessageService _service = null, Logger _logger = null, Stats _stats = null)
        {
            if (string.IsNullOrEmpty(_pipeName))
            {
                throw new ArgumentException("PipeName is null or empty.");
            }

            PipeName = _pipeName;

            Service = _service != null ? _service : new IPCService();
            Service.OnMessageReceived += messageReceived_relay;

            Logger = _logger != null ? _logger : new Logger(logLevel: ServiceWire.LogLevel.Warn);
            Stats = _stats != null ? _stats : new Stats();

            NpHost = new NpHost(PipeName, Logger, Stats);
            NpHost.AddService(Service);
            NpHost.Open();
        }

        public void Dispose()
        {
            NpHost?.Close();
        }

        public void Open()
        {
            NpHost?.Open();
        }

        public void Close()
        {
            NpHost?.Close();
        }

        private void messageReceived_relay(IPCMessage message)
        {
            OnMessageReceived?.Invoke(message);
        }

        public Guid SendMessage(IPCMessage message)
        {
            return Service.SendMessage(message);
        }

        public IPCMessage GetMessage(Guid messageId)
        {
            return Service.GetMessage(messageId);
        }

        public void SendResponse(IPCMessage message)
        {
            Service.SendResponse(message);
        }

        public IPCMessage GetResponse(Guid responseId)
        {
            return Service.GetResponse(responseId);
        }
    }

    public class IPCNamedPipeClient : IMessageService, IDisposable
    {
        public string PipeName { get; set; }
        public NpClient<IMessageService> NpClient { get; set; }
        public IMessageService Service { get; set; }

        public event IPCMessageDelegate OnMessageReceived;

        public IPCNamedPipeClient(string _pipeName)
        {
            if (string.IsNullOrEmpty(_pipeName))
            {
                throw new ArgumentException("PipeName is null or empty.");
            }

            PipeName = _pipeName;
            NpClient = new NpClient<IMessageService>(new NpEndPoint(PipeName));
        }

        public void Dispose()
        {
            NpClient?.Dispose();
            NpClient = null;
        }

        private void verifyConnection()
        {
            if (NpClient == null || !NpClient.IsConnected)
            {
                if (!NpClient.IsConnected)
                {
                    NpClient.Dispose();
                    NpClient = null;
                }
                NpClient = new NpClient<IMessageService>(new NpEndPoint(PipeName));
            }
        }

        public Guid SendMessage(IPCMessage message)
        {
            verifyConnection();
            return NpClient.Proxy.SendMessage(message);
        }

        public IPCMessage GetMessage(Guid messageId)
        {
            verifyConnection();
            return NpClient.Proxy.GetMessage(messageId);
        }

        public void SendResponse(IPCMessage message)
        {
            verifyConnection();
            NpClient.Proxy.SendResponse(message);
        }

        public IPCMessage GetResponse(Guid responseId)
        {
            verifyConnection();
            return NpClient.Proxy.GetResponse(responseId);
        }
    }
}
