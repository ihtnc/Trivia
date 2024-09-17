namespace Trivia.Common
{
    public enum TcpClientMessageType : int
    {
        Unknown = default,
        Request = 'R',
        Connect = 'C',
        Disconnect = 'D',
        RequestRoundDetails = 'S',
        TriviaAnswer = 'A'
    }

    /// <summary>
    /// Represents a message sent by a TCP client.
    /// </summary>
    public class TcpClientMessage
    {
        /// <summary>
        /// The type of message.
        /// </summary>
        public virtual TcpClientMessageType MessageType { get; private set; } = TcpClientMessageType.Unknown;

        /// <summary>
        /// The payload of the message.
        /// </summary>
        public string Payload { get; protected set; } = string.Empty;

        /// <summary>
        /// Creates a new <see cref="TcpClientMessage"/> with the specified <paramref name="messageType"/> and <paramref name="payload"/>.
        /// </summary>
        public static TcpClientMessage Create(TcpClientMessageType messageType, string payload)
        {
            return new TcpClientMessage
            {
                MessageType = messageType,
                Payload = payload
            };
        }

        /// <summary>
        /// Converts the message to a string.
        /// </summary>
        public override string ToString()
        {
            return $"{(char)MessageType}{Payload}";
        }

        /// <summary>
        /// Gets the payload of the message.
        /// </summary>
        public virtual string GetPayload() => Payload;
    }

    /// <summary>
    /// Represents a message sent by a TCP client when requesting a connection to the sender endpoint.
    /// </summary>
    public class RequestConnectionClientMessage : TcpClientMessage
    {
        public override TcpClientMessageType MessageType => TcpClientMessageType.Request;

        /// <summary>
        /// The value on the payload representing the name of the client.
        /// </summary>
        public string Name { get; protected set; } = string.Empty;

        /// <summary>
        /// Creates a new <see cref="RequestConnectionClientMessage"/> with the specified <paramref name="name"/>.
        /// </summary>
        /// <param name="name">
        /// The name of the client.
        /// </param>
        public static RequestConnectionClientMessage Create(string name)
        {
            return new RequestConnectionClientMessage
            {
                Name = name,
                Payload = name.Base64Encode()
            };
        }

        /// <summary>
        /// Creates a new <see cref="RequestConnectionClientMessage"/> with values from the specified <paramref name="payload"/>.
        /// </summary>
        /// <param name="payload">The payload to create the message from.</param>
        /// <returns>
        /// A new <see cref="RequestConnectionClientMessage"/> if the <paramref name="payload"/> is valid; otherwise, null.
        /// </returns>
        public static RequestConnectionClientMessage? CreateFromPayload(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload)) { return null; }

            return new RequestConnectionClientMessage
            {
                Name = payload.Base64Decode(),
                Payload = payload
            };
        }

        public override string GetPayload() => Name.Base64Encode();
    }

    /// <summary>
    /// Represents a message sent by a TCP client when establishing a connection to the sender endpoint.
    /// </summary>
    public class EstablishConnectionClientMessage : TcpClientMessage
    {
        public override TcpClientMessageType MessageType => TcpClientMessageType.Connect;

        /// <summary>
        /// The Id assigned to the client.
        /// </summary>
        public int ClientId { get; protected set; }

        /// <summary>
        /// The name of the client.
        /// </summary>
        public string Name { get; protected set; } = string.Empty;

        /// <summary>
        /// Creates a new <see cref="EstablishConnectionClientMessage"/> with the specified <paramref name="clientId"/> and <paramref name="name"/>.
        /// </summary>
        /// <param name="clientId">
        /// The Id assigned to the client.
        /// </param>
        /// <param name="name">
        /// The name of the client.
        /// </param>
        public static EstablishConnectionClientMessage Create(int clientId, string name)
        {
            var message = new EstablishConnectionClientMessage
            {
                ClientId = clientId,
                Name = name
            };

            message.Payload = message.GetPayload();

            return message;
        }

        /// <summary>
        /// Creates a new <see cref="EstablishConnectionClientMessage"/> with values from the specified <paramref name="payload"/>.
        /// </summary>
        /// <param name="payload">The payload to create the message from.</param>
        /// <returns>
        /// A new <see cref="EstablishConnectionClientMessage"/> if the <paramref name="payload"/> is valid; otherwise, null.
        /// </returns>
        public static EstablishConnectionClientMessage? CreateFromPayload(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload)) { return null; }

            var parts = payload.Split(Separator);
            if (parts.Length != 2) { return null; }
            if (!int.TryParse(parts[0], out var clientId)) { return null; }
            if (string.IsNullOrWhiteSpace(parts[1])) { return null; }

            return new EstablishConnectionClientMessage
            {
                ClientId = clientId,
                Name = parts[1].Base64Decode(),
                Payload = payload
            };
        }

        public override string GetPayload() => $"{ClientId}{Separator}{Name.Base64Encode()}";

        private const string Separator = "|";
    }

    /// <summary>
    /// Represents a message sent by a TCP client when disconnecting from the sender endpoint.
    /// </summary>
    public class DisconnectClientMessage : TcpClientMessage
    {
        public override TcpClientMessageType MessageType => TcpClientMessageType.Disconnect;

        /// <summary>
        /// The Id assigned to the client.
        /// </summary>
        public int ClientId { get; protected set; }

        /// <summary>
        /// The name of the client.
        /// </summary>
        public string Name { get; protected set; } = string.Empty;

        /// <summary>
        /// Creates a new <see cref="DisconnectClientMessage"/> with the specified <paramref name="clientId"/>.
        /// </summary>
        /// <param name="clientId">
        /// The Id assigned to the client.
        /// </param>
        public static DisconnectClientMessage Create(int clientId, string name)
        {
            var message = new DisconnectClientMessage
            {
                ClientId = clientId,
                Name = name
            };

            message.Payload = message.GetPayload();

            return message;
        }

        /// <summary>
        /// Creates a new <see cref="DisconnectClientMessage"/> with values from the specified <paramref name="payload"/>.
        /// </summary>
        /// <param name="payload">The payload to create the message from.</param>
        /// <returns>
        /// A new <see cref="DisconnectClientMessage"/> if the <paramref name="payload"/> is valid; otherwise, null.
        /// </returns>
        public static DisconnectClientMessage? CreateFromPayload(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload)) { return null; }

            var parts = payload.Split(Separator);
            if (parts.Length != 2) { return null; }
            if (!int.TryParse(parts[0], out var clientId)) { return null; }
            if (string.IsNullOrWhiteSpace(parts[1])) { return null; }

            return new DisconnectClientMessage
            {
                ClientId = clientId,
                Name = parts[1].Base64Decode(),
                Payload = payload
            };
        }

        public override string GetPayload() => $"{ClientId}{Separator}{Name.Base64Encode()}";

        private const string Separator = "|";
    }

    /// <summary>
    /// Represents a message sent by a TCP client when answering a trivia question.
    /// </summary>
    public class TriviaAnswerClientMessage : TcpClientMessage
    {
        public override TcpClientMessageType MessageType => TcpClientMessageType.TriviaAnswer;

        /// <summary>
        /// The Id of the round the answer is for.
        /// </summary>
        public int RoundId { get; protected set; }

        /// <summary>
        /// The Id of the question the answer is for.
        /// </summary>
        public int QuestionId { get; protected set; }

        /// <summary>
        /// The Id assigned to the client.
        /// </summary>
        public int ClientId { get; protected set; }

        /// <summary>
        /// The name of the client.
        /// </summary>
        public string Name { get; protected set; } = string.Empty;

        /// <summary>
        /// The index of the answer to the question.
        /// </summary>
        public int AnswerIndex { get; protected set; }

        /// <summary>
        /// Creates a new <see cref="TriviaAnswerClientMessage"/> with the specified <paramref name="roundId"/>, <paramref name="questionId"/>, <paramref name="clientId"/>, <paramref name="name"/>, and <paramref name="answerIndex"/>.
        /// </summary>
        /// <param name="roundId">The Id of the round the answer is for.</param>
        /// <param name="questionId">The Id of the question the answer is for.</param>
        /// <param name="clientId">The Id assigned to the client.</param>
        /// <param name="name">The name of the client.</param>
        /// <param name="answerIndex">The index of the answer to the question.</param>
        public static TriviaAnswerClientMessage Create(int roundId, int questionId, int clientId, string name, int answerIndex)
        {
            var message = new TriviaAnswerClientMessage
            {
                RoundId = roundId,
                QuestionId = questionId,
                ClientId = clientId,
                Name = name,
                AnswerIndex = answerIndex
            };

            message.Payload = message.GetPayload();

            return message;
        }

        /// <summary>
        /// Creates a new <see cref="TriviaAnswerClientMessage"/> with values from the specified <paramref name="payload"/>.
        /// </summary>
        /// <param name="payload">The payload to create the message from.</param>
        /// <returns>
        /// A new <see cref="TriviaAnswerClientMessage"/> if the <paramref name="payload"/> is valid; otherwise, null.
        /// </returns>
        public static TriviaAnswerClientMessage? CreateFromPayload(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload)) { return null; }

            var parts = payload.Split(Separator);
            if (parts.Length != 5) { return null; }
            if (!int.TryParse(parts[0], out var roundId)) { return null; }
            if (!int.TryParse(parts[1], out var questionId)) { return null; }
            if (!int.TryParse(parts[2], out var clientId)) { return null; }
            if (string.IsNullOrWhiteSpace(parts[3])) { return null; }
            if (!int.TryParse(parts[4], out var answerIndex)) { return null; }

            return new TriviaAnswerClientMessage
            {
                RoundId = roundId,
                QuestionId = questionId,
                ClientId = clientId,
                Name = parts[3].Base64Decode(),
                AnswerIndex = answerIndex,
                Payload = payload
            };
        }

        public override string GetPayload() => $"{RoundId}{Separator}{QuestionId}{Separator}{ClientId}{Separator}{Name.Base64Encode()}{Separator}{AnswerIndex}";

        private const string Separator = "|";
    }

    /// <summary>
    /// Represents a message sent by a TCP client when requesting details about the current round.
    /// </summary>
    public class RequestRoundDetailsClientMessage : TcpClientMessage
    {
        public override TcpClientMessageType MessageType => TcpClientMessageType.RequestRoundDetails;

        /// <summary>
        /// The Id assigned to the client.
        /// </summary>
        public int ClientId { get; protected set; }

        /// <summary>
        /// The name of the client.
        /// </summary>
        public string Name { get; protected set; } = string.Empty;

        /// <summary>
        /// Creates a new <see cref="RequestRoundDetailsClientMessage"/> with the specified <paramref name="clientId"/>, and <paramref name="name"/>.
        /// </summary>
        /// <param name="clientId">
        /// The Id assigned to the client.
        /// </param>
        /// <param name="name">
        /// The name of the client.
        /// </param>
        public static RequestRoundDetailsClientMessage Create(int clientId, string name)
        {
            var message = new RequestRoundDetailsClientMessage
            {
                ClientId = clientId,
                Name = name
            };

            message.Payload = message.GetPayload();

            return message;
        }

        /// <summary>
        /// Creates a new <see cref="RequestRoundDetailsClientMessage"/> with values from the specified <paramref name="payload"/>.
        /// </summary>
        /// <param name="payload">The payload to create the message from.</param>
        /// <returns>
        /// A new <see cref="RequestRoundDetailsClientMessage"/> if the <paramref name="payload"/> is valid; otherwise, null.
        /// </returns>
        public static RequestRoundDetailsClientMessage? CreateFromPayload(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload)) { return null; }

            var parts = payload.Split(Separator);
            if (parts.Length != 2) { return null; }
            if (!int.TryParse(parts[0], out var clientId)) { return null; }
            if (string.IsNullOrWhiteSpace(parts[1])) { return null; }

            return new RequestRoundDetailsClientMessage
            {
                ClientId = clientId,
                Name = parts[1].Base64Decode(),
                Payload = payload
            };
        }

        public override string GetPayload() => $"{ClientId}{Separator}{Name.Base64Encode()}";

        private const string Separator = "|";
    }
}