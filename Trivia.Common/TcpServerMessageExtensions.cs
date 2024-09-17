namespace Trivia.Common
{
    public static class TcpServerMessageExtensions
    {
        /// <summary>
        /// Converts a <see cref="TcpServerMessage"/> to a <see cref="SetupConnectionServerMessage"/>.
        /// </summary>
        /// <param name="message">The <see cref="TcpServerMessage"/> to convert.</param>
        /// <returns>
        /// A <see cref="SetupConnectionServerMessage"/> if the <paramref name="message"/> is a setup connection message; otherwise, null.
        /// </returns>
        public static SetupConnectionServerMessage? ToSetupConnection(this TcpServerMessage message)
        {
            if (message is SetupConnectionServerMessage initialMessage)
            {
                return initialMessage;
            }

            if (message.MessageType != TcpServerMessageType.SetupConnection) { return null; }

            return SetupConnectionServerMessage.CreateFromPayload(message.Payload);
        }

        /// <summary>
        /// Converts a <see cref="TcpServerMessage"/> to a <see cref="TriviaRoundQuestionServerMessage"/>.
        /// </summary>
        /// <param name="message">The <see cref="TcpServerMessage"/> to convert.</param>
        /// <returns>
        /// A <see cref="TriviaRoundQuestionServerMessage"/> if the <paramref name="message"/> is a trivia round question message; otherwise, null.
        /// </returns>
        public static TriviaRoundStartServerMessage? ToTriviaRoundStart(this TcpServerMessage message)
        {
            if (message is TriviaRoundStartServerMessage initialMessage)
            {
                return initialMessage;
            }

            if (message.MessageType != TcpServerMessageType.RoundStart) { return null; }

            return TriviaRoundStartServerMessage.CreateFromPayload(message.Payload);
        }

        /// <summary>
        /// Converts a <see cref="TcpServerMessage"/> to a <see cref="TriviaRoundQuestionServerMessage"/>
        /// </summary>
        /// <param name="message">The <see cref="TcpServerMessage"/> to convert.</param>
        /// <returns>
        /// A <see cref="TriviaRoundQuestionServerMessage"/> if the <paramref name="message"/> is a trivia round question message; otherwise, null.
        /// </returns>
        public static TriviaQuestionServerMessage? ToTriviaQuestion(this TcpServerMessage message)
        {
            if (message is TriviaQuestionServerMessage initialMessage)
            {
                return initialMessage;
            }

            if (message.MessageType != TcpServerMessageType.Question) { return null; }

            return TriviaQuestionServerMessage.CreateFromPayload(message.Payload);
        }

        /// <summary>
        /// Converts a <see cref="TcpServerMessage"/> to a <see cref="TriviaQuestionResultServerMessage"/>
        /// </summary>
        /// <param name="message">The <see cref="TcpServerMessage"/> to convert.</param>
        /// <returns>
        /// A <see cref="TriviaQuestionResultServerMessage"/> if the <paramref name="message"/> is a trivia question result message; otherwise, null.
        /// </returns>
        public static TriviaQuestionResultServerMessage? ToTriviaQuestionResult(this TcpServerMessage message)
        {
            if (message is TriviaQuestionResultServerMessage initialMessage)
            {
                return initialMessage;
            }

            if (message.MessageType != TcpServerMessageType.Result) { return null; }

            return TriviaQuestionResultServerMessage.CreateFromPayload(message.Payload);
        }

        /// <summary>
        /// Converts a <see cref="TcpServerMessage"/> to a <see cref="TriviaRoundEndServerMessage"/>
        /// </summary>
        /// <param name="message">The <see cref="TcpServerMessage"/> to convert.</param>
        /// <returns>
        /// A <see cref="TriviaRoundEndServerMessage"/> if the <paramref name="message"/> is a trivia round end message; otherwise, null.
        /// </returns>
        public static TriviaRoundEndServerMessage? ToTriviaRoundEnd(this TcpServerMessage message)
        {
            if (message is TriviaRoundEndServerMessage initialMessage)
            {
                return initialMessage;
            }

            if (message.MessageType != TcpServerMessageType.RoundEnd) { return null; }

            return TriviaRoundEndServerMessage.CreateFromPayload(message.Payload);
        }

        /// <summary>
        /// Converts a <see cref="TcpServerMessage"/> to a <see cref="TriviaRoundDetailsServerMessage"/>
        /// </summary>
        /// <param name="message">The <see cref="TcpServerMessage"/> to convert.</param>
        /// <returns>
        /// A <see cref="TriviaRoundDetailsServerMessage"/> if the <paramref name="message"/> is a trivia round details message; otherwise, null.
        /// </returns>
        public static TriviaRoundDetailsServerMessage? ToTriviaRoundDetails(this TcpServerMessage message)
        {
            if (message is TriviaRoundDetailsServerMessage initialMessage)
            {
                return initialMessage;
            }

            if (message.MessageType != TcpServerMessageType.RoundDetails) { return null; }

            return TriviaRoundDetailsServerMessage.CreateFromPayload(message.Payload);
        }

        /// <summary>
        /// Converts a <see cref="TcpServerMessage"/> to an <see cref="AcceptedServerMessage"/>.
        /// </summary>
        /// <param name="message">The <see cref="TcpServerMessage"/> to convert.</param>
        /// <returns>An <see cref="AcceptedServerMessage"/> if the <paramref name="message"/> is an accepted message; otherwise, null.</returns>
        public static AcceptedServerMessage? ToAccepted(this TcpServerMessage message)
        {
            if (message is AcceptedServerMessage initialMessage)
            {
                return initialMessage;
            }

            if (message.MessageType != TcpServerMessageType.Accepted) { return null; }

            return AcceptedServerMessage.Create();
        }

        /// <summary>
        /// Converts a <see cref="TcpServerMessage"/> to an <see cref="ErrorServerMessage"/>.
        /// </summary>
        /// <param name="message">The <see cref="TcpServerMessage"/> to convert.</param>
        /// <returns>An <see cref="ErrorServerMessage"/> if the <paramref name="message"/> is an error message; otherwise, null.</returns>
        public static ErrorServerMessage? ToError(this TcpServerMessage message)
        {
            if (message is ErrorServerMessage initialMessage)
            {
                return initialMessage;
            }

            if (message.MessageType != TcpServerMessageType.Error) { return null; }

            return ErrorServerMessage.CreateFromPayload(message.Payload);
        }
    }
}