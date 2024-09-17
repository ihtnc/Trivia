using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace Trivia.Common
{
    public static class TcpClientExtensions
    {
        /// <summary>
        /// Reads a message from the TcpClient
        /// </summary>
        /// <param name="client">The TcpClient to read from</param>
        /// <param name="wait">Whether to wait for a message to be available</param>
        /// <param name="waitDelayMs">The delay in milliseconds to wait between checks for a message</param>
        /// <param name="timeoutMs">The timeout in milliseconds to wait for a message</param>
        /// <returns>A <see cref="TcpClientMessage"/> if a message was read, otherwise null</returns>
        public static async Task<TcpClientMessage?> ReadFromClientAsync(this TcpClient client, bool wait = false, int waitDelayMs = 100, int timeoutMs = 5000, CancellationToken cancellationToken = default)
        {
            if (!wait && client.Available == 0) { return null; }

            waitDelayMs = waitDelayMs < 0 ? 100 : waitDelayMs;
            timeoutMs = timeoutMs < 0 ? 5000 : timeoutMs;

            var timeout = DateTime.Now.AddMilliseconds(timeoutMs);
            while(client.Available == 0)
            {
                await Task.Delay(waitDelayMs, cancellationToken);
                if (DateTime.Now > timeout) { return null; }
            }

            var stream = client.GetStream();
            var buffer = new byte[1];
            var byteRead = await stream.ReadAsync(buffer, cancellationToken);

            var header = Encoding.UTF8.GetChars(buffer, 0, byteRead);

            var messageType = (TcpClientMessageType)header[0];
            if (messageType == TcpClientMessageType.Unknown) { return null; }

            var payloadBuffer = new List<byte>();
            byteRead = await stream.ReadAsync(buffer, cancellationToken);

            while(byteRead != 0 && buffer[0] != '\0')
            {
                payloadBuffer.Add(buffer[0]);
                byteRead = await stream.ReadAsync(buffer, cancellationToken);
            }

            var message = Encoding.UTF8.GetString(payloadBuffer.ToArray());
            return TcpClientMessage.Create(messageType, message);
        }

        /// <summary>
        /// Reads an establish connection message from the TcpClient
        /// </summary>
        /// <param name="client">The TcpClient to read from</param>
        /// <param name="wait">Whether to wait for a message to be available</param>
        /// <param name="waitDelayMs">The delay in milliseconds to wait between checks for a message</param>
        /// <param name="timeoutMs">The timeout in milliseconds to wait for a message</param>
        /// <returns>An <see cref="EstablishConnectionClientMessage"/> if an establish connection message was read, otherwise null</returns>
        public static async Task<EstablishConnectionClientMessage?> ReadEstablishConnectionFromClientAsync(this TcpClient client, bool wait = false, int waitDelayMs = 100, int timeoutMs = 5000, CancellationToken cancellationToken = default)
        {
            var message = await client.ReadFromClientAsync(wait, waitDelayMs, timeoutMs, cancellationToken);
            return message?.ToEstablishConnection();
        }

        /// <summary>
        /// Sends a message to the TcpClient
        /// </summary>
        /// <param name="client">The TcpClient to send the message to</param>
        /// <param name="message">The message to send</param>
        public static async Task SendToClientAsync(this TcpClient client, TcpServerMessage message, CancellationToken cancellationToken = default)
        {
            var stream = client.GetStream();
            var buffer = Encoding.UTF8.GetBytes($"{(char)message.MessageType}{message.Payload}\0");
            await stream.WriteAsync(buffer, cancellationToken);
        }

        /// <summary>
        /// Sends a setup connection message to the TcpClient
        /// </summary>
        /// <param name="client">The TcpClient to send the message to</param>
        /// <param name="clientId">The Id assigned to the client</param>
        /// <param name="sender">The endpoint of the sender to connect to</param>
        public static async Task SendSetupConnectionToClientAsync(this TcpClient client, int clientId, IPEndPoint sender, CancellationToken cancellationToken = default)
        {
            var message = SetupConnectionServerMessage.Create(clientId, sender);
            await client.SendToClientAsync(message, cancellationToken);
        }

        /// <summary>
        /// Sends an accepted message to the TcpClient
        /// </summary>
        /// <param name="client">The TcpClient to send the message to</param>
        public static async Task SendAcceptedToClientAsync(this TcpClient client, CancellationToken cancellationToken = default)
        {
            var message = AcceptedServerMessage.Create();
            await client.SendToClientAsync(message, cancellationToken);
        }

        /// <summary>
        /// Sends an error message to the TcpClient
        /// </summary>
        /// <param name="client">The TcpClient to send the message to</param>
        /// <param name="error">The error message to send</param>
        public static async Task SendErrorToClientAsync(this TcpClient client, string error, CancellationToken cancellationToken = default)
        {
            var message = ErrorServerMessage.Create(error);
            await client.SendToClientAsync(message, cancellationToken);
        }

        /// <summary>
        /// Sends a message to the TcpClient
        /// </summary>
        /// <param name="client">The TcpClient to send the message to</param>
        /// <param name="message">The message to send</param>
        public static async Task SendToServerAsync(this TcpClient client, TcpClientMessage message, CancellationToken cancellationToken = default)
        {
            var stream = client.GetStream();
            var buffer = Encoding.UTF8.GetBytes($"{(char)message.MessageType}{message.Payload}\0");
            await stream.WriteAsync(buffer, cancellationToken);
        }

        /// <summary>
        /// Sends a request connection message to the TcpClient
        /// </summary>
        /// <param name="client">The TcpClient to send the message to</param>
        /// <param name="name">The name of the client to connect to the server</param>
        public static async Task SendRequestConnectionToServerAsync(this TcpClient client, string name, CancellationToken cancellationToken = default)
        {
            var message = RequestConnectionClientMessage.Create(name);
            await client.SendToServerAsync(message, cancellationToken);
        }

        /// <summary>
        /// Sends an establish connection message to the TcpClient
        /// </summary>
        /// <param name="client">The TcpClient to send the message to</param>
        /// <param name="clientId">The Id assigned to the client</param>
        /// <param name="name">The name of the client to connect to the server</param>
        public static async Task SendEstablishConnectionToServerAsync(this TcpClient client, int clientId, string name, CancellationToken cancellationToken = default)
        {
            var message = EstablishConnectionClientMessage.Create(clientId, name);
            await client.SendToServerAsync(message, cancellationToken);
        }

        /// <summary>
        /// Reads a message from the TcpClient
        /// </summary>
        /// <param name="client">The TcpClient to read from</param>
        /// <param name="wait">Whether to wait for a message to be available</param>
        /// <param name="waitDelayMs">The delay in milliseconds to wait between checks for a message</param>
        /// <param name="timeoutMs">The timeout in milliseconds to wait for a message</param>
        /// <returns>A <see cref="TcpServerMessage"/> if a message was read, otherwise null</returns>
        public static async Task<TcpServerMessage?> ReadFromServerAsync(this TcpClient client, bool wait = false, int waitDelayMs = 100, int timeoutMs = 5000, CancellationToken cancellationToken = default)
        {
            if (!wait && client.Available == 0) { return null; }

            waitDelayMs = waitDelayMs < 0 ? 100 : waitDelayMs;
            timeoutMs = timeoutMs < 0 ? 5000 : timeoutMs;

            var timeout = DateTime.Now.AddMilliseconds(timeoutMs);
            while(client.Available == 0)
            {
                await Task.Delay(waitDelayMs, cancellationToken);
                if (DateTime.Now > timeout) { return null; }
            }

            var stream = client.GetStream();
            var buffer = new byte[1];
            var byteRead = await stream.ReadAsync(buffer, cancellationToken);

            var header = Encoding.UTF8.GetChars(buffer, 0, byteRead);

            var messageType = (TcpServerMessageType)header[0];
            if (messageType == TcpServerMessageType.Unknown) { return null; }

            var payloadBuffer = new List<byte>();
            byteRead = await stream.ReadAsync(buffer, cancellationToken);

            while (byteRead != 0 && buffer[0] != '\0')
            {
                payloadBuffer.Add(buffer[0]);
                byteRead = await stream.ReadAsync(buffer, cancellationToken);
            }

            var message = Encoding.UTF8.GetString(payloadBuffer.ToArray());
            return TcpServerMessage.Create(messageType, message);
        }

        /// <summary>
        /// Reads a setup connection message from the TcpClient
        /// </summary>
        /// <param name="client">The TcpClient to read from</param>
        /// <param name="wait">Whether to wait for a message to be available</param>
        /// <param name="waitDelayMs">The delay in milliseconds to wait between checks for a message</param>
        /// <param name="timeoutMs">The timeout in milliseconds to wait for a message</param>
        /// <returns>An <see cref="SetupConnectionServerMessage"/> if an establish connection message was read, otherwise null</returns>
        public static async Task<SetupConnectionServerMessage?> ReadSetupConnectionFromServerAsync(this TcpClient client, bool wait = false, int waitDelayMs = 100, int timeoutMs = 5000, CancellationToken cancellationToken = default)
        {
            var message = await client.ReadFromServerAsync(wait, waitDelayMs, timeoutMs, cancellationToken);
            return message?.ToSetupConnection();
        }

        /// <summary>
        /// Reads an accepted message from the TcpClient
        /// </summary>
        /// <param name="client">The TcpClient to read from</param>
        /// <param name="wait">Whether to wait for a message to be available</param>
        /// <param name="waitDelayMs">The delay in milliseconds to wait between checks for a message</param>
        /// <param name="timeoutMs">The timeout in milliseconds to wait for a message</param>
        /// <returns>An <see cref="AcceptedServerMessage"/> if an accepted message was read, otherwise null</returns>
        public static async Task<AcceptedServerMessage?> ReadAcceptedFromServerAsync(this TcpClient client, bool wait = false, int waitDelayMs = 100, int timeoutMs = 5000, CancellationToken cancellationToken = default)
        {
            var message = await client.ReadFromServerAsync(wait, waitDelayMs, timeoutMs, cancellationToken);
            return message?.ToAccepted();
        }

        public static bool CheckRemoteConnection(this TcpClient client)
        {
            if (!client.Connected) { return false; }

            var ipProperties = IPGlobalProperties.GetIPGlobalProperties();
            var tcpConnections = ipProperties
                .GetActiveTcpConnections()
                .Where(x => x.LocalEndPoint.Equals(client.Client.LocalEndPoint) && x.RemoteEndPoint.Equals(client.Client.RemoteEndPoint))
                .ToArray();

            if (tcpConnections == null || tcpConnections.Length == 0) { return false; }

            TcpState stateOfConnection = tcpConnections.First().State;
            return stateOfConnection == TcpState.Established;
        }
    }
}