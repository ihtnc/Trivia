namespace Trivia.Common
{
    public static class TcpClientMessageExtensions
    {
        /// <summary>
        /// Converts a <see cref="TcpClientMessage"/> to an <see cref="RequestConnectionClientMessage"/>.
        /// </summary>
        /// <param name="message">The <see cref="TcpClientMessage"/> to convert.</param>
        /// <returns>
        /// An <see cref="RequestConnectionClientMessage"/> if the <paramref name="message"/> is a request connection message; otherwise, null.
        /// </returns>
        public static RequestConnectionClientMessage? ToRequestConnection(this TcpClientMessage message)
        {
            if (message is RequestConnectionClientMessage initialMessage)
            {
                return initialMessage;
            }

            if (message.MessageType != TcpClientMessageType.Request) { return null; }

            return RequestConnectionClientMessage.CreateFromPayload(message.Payload);
        }

        /// <summary>
        /// Converts a <see cref="TcpClientMessage"/> to an <see cref="EstablishConnectionClientMessage"/>.
        /// </summary>
        /// <param name="message">The <see cref="TcpClientMessage"/> to convert.</param>
        /// <returns>
        /// An <see cref="EstablishConnectionClientMessage"/> if the <paramref name="message"/> is an establish connection message; otherwise, null.
        /// </returns>
        public static EstablishConnectionClientMessage? ToEstablishConnection(this TcpClientMessage message)
        {
            if (message is EstablishConnectionClientMessage initialMessage)
            {
                return initialMessage;
            }

            if (message.MessageType != TcpClientMessageType.Connect) { return null; }

            return EstablishConnectionClientMessage.CreateFromPayload(message.Payload);
        }

        /// <summary>
        /// Converts a <see cref="TcpClientMessage"/> to a <see cref="DisconnectClientMessage"/>.
        /// </summary>
        /// <param name="message">The <see cref="TcpClientMessage"/> to convert.</param>
        /// <returns>
        /// A <see cref="DisconnectClientMessage"/> if the <paramref name="message"/> is a disconnect message; otherwise, null.
        /// </returns>
        public static DisconnectClientMessage? ToDisconnect(this TcpClientMessage message)
        {
            if (message is DisconnectClientMessage initialMessage)
            {
                return initialMessage;
            }

            if (message.MessageType != TcpClientMessageType.Disconnect) { return null; }

            return DisconnectClientMessage.CreateFromPayload(message.Payload);
        }

        /// <summary>
        /// Converts a <see cref="TcpClientMessage"/> to a <see cref="TriviaAnswerClientMessage"/>.
        /// </summary>
        /// <param name="message">The <see cref="TcpClientMessage"/> to convert.</param>
        /// <returns>
        /// A <see cref="TriviaAnswerClientMessage"/> if the <paramref name="message"/> is a trivia answer message; otherwise, null.
        /// </returns>
        public static TriviaAnswerClientMessage? ToTriviaAnswer(this TcpClientMessage message)
        {
            if (message is TriviaAnswerClientMessage initialMessage)
            {
                return initialMessage;
            }

            if (message.MessageType != TcpClientMessageType.TriviaAnswer) { return null; }

            return TriviaAnswerClientMessage.CreateFromPayload(message.Payload);
        }

        /// <summary>
        /// Converts a <see cref="TcpClientMessage"/> to a <see cref="RequestRoundDetailsClientMessage"/>.
        /// </summary>
        /// <param name="message">The <see cref="TcpClientMessage"/> to convert.</param>
        /// <returns>
        /// A <see cref="RequestRoundDetailsClientMessage"/> if the <paramref name="message"/> is a request round details message; otherwise, null.
        /// </returns>
        public static RequestRoundDetailsClientMessage? ToRequestRoundDetails(this TcpClientMessage message)
        {
            if (message is RequestRoundDetailsClientMessage initialMessage)
            {
                return initialMessage;
            }

            if (message.MessageType != TcpClientMessageType.RequestRoundDetails) { return null; }

            return RequestRoundDetailsClientMessage.CreateFromPayload(message.Payload);
        }
    }
}