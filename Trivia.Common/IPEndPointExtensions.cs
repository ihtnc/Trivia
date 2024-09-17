using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Trivia.Common
{
    public static class IPEndPointExtensions
    {
        /// <summary>
        /// Sends a message to the IPEndPoint
        /// </summary>
        /// <param name="endpoint">The IpEndPoint to send the message to</param>
        /// <param name="message">The message to send</param>
        public static async Task<TcpServerMessage> SendToServerAsync(this IPEndPoint endpoint, TcpClientMessage message, CancellationToken cancellationToken = default)
        {
            using var client = new TcpClient();
            client.Connect(endpoint);
            if (!client.Connected) { throw new Exception($"Unable to connect to endpoint {endpoint}."); }

            var stream = client.GetStream();
            var buffer = Encoding.UTF8.GetBytes($"{(char)message.MessageType}{message.Payload}\0");
            await stream.WriteAsync(buffer, cancellationToken);

            var response = await client.ReadFromServerAsync(wait: true, cancellationToken: cancellationToken);
            return response ?? throw new Exception($"Invalid response from endpoint {endpoint}.");
        }

        /// <summary>
        /// Sends an answer message to the IPEndPoint
        /// </summary>
        /// <param name="endpoint">The IpEndPoint to send the message to</param>
        /// <param name="roundId">The Id of the round the question is from</param>
        /// <param name="questionId">The Id of the question the answer is for</param>
        /// <param name="clientId">The Id assigned to the client</param>
        /// <param name="name">The name of the client</param>
        /// <param name="answerIndex">The index of the answer</param>
        public static async Task<TcpServerMessage> SendAnswerToServerAsync(this IPEndPoint endpoint, int roundId, int questionId, int clientId, string name, int answerIndex, CancellationToken cancellationToken = default)
        {
            var message = TriviaAnswerClientMessage.Create(roundId, questionId, clientId, name, answerIndex);
            return await endpoint.SendToServerAsync(message, cancellationToken);
        }

        /// <summary>
        /// Sends a disconnect message to the IPEndPoint
        /// </summary>
        /// <param name="endpoint">The IpEndPoint to send the message to</param>
        /// <param name="clientId">The Id assigned to the client</param>
        /// <param name="name">The name of the client</param>
        public static async Task<TcpServerMessage> SendDisconnectToServerAsync(this IPEndPoint endpoint, int clientId, string name, CancellationToken cancellationToken = default)
        {
            var message = DisconnectClientMessage.Create(clientId, name);
            return await endpoint.SendToServerAsync(message, cancellationToken);
        }

        /// <summary>
        /// Sends a request round details message to the IPEndPoint
        /// </summary>
        /// <param name="endpoint">The IpEndPoint to send the message to</param>
        /// <param name="clientId">The Id assigned to the client</param>
        /// <param name="name">The name of the client</param>
        public static async Task<TcpServerMessage> SendRequestRoundDetailsToServerAsync(this IPEndPoint endpoint, int clientId, string name, CancellationToken cancellationToken = default)
        {
            var message = RequestRoundDetailsClientMessage.Create(clientId, name);
            return await endpoint.SendToServerAsync(message, cancellationToken);
        }
    }
}
