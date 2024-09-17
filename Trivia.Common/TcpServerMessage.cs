using System.Net;

namespace Trivia.Common
{
    public enum TcpServerMessageType : int
    {
        Unknown = 0,
        SetupConnection = 'C',
        Accepted = 'A',
        RoundDetails = 'D',
        RoundStart = 'S',
        Question = 'Q',
        Result = 'R',
        RoundEnd = 'E',
        Error = 'X'
    }

    /// <summary>
    /// Represents a message sent by a TCP server.
    /// </summary>
    public class TcpServerMessage
    {
        /// <summary>
        /// The type of message.
        /// </summary>
        public virtual TcpServerMessageType MessageType { get; private set; } = TcpServerMessageType.Unknown;

        /// <summary>
        /// The payload of the message.
        /// </summary>
        public string Payload { get; protected set; } = string.Empty;

        /// <summary>
        /// Creates a new <see cref="TcpServerMessage"/> with the specified <paramref name="messageType"/> and <paramref name="payload"/>.
        /// </summary>
        public static TcpServerMessage Create(TcpServerMessageType messageType, string payload)
        {
            return new TcpServerMessage
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
    /// Represents a message sent by a TCP server for clients to establish a connection to the sender endpoint.
    /// </summary>
    public class SetupConnectionServerMessage : TcpServerMessage
    {
        public override TcpServerMessageType MessageType => TcpServerMessageType.SetupConnection;

        /// <summary>
        /// The Id assigned to the client.
        /// </summary>
        public int ClientId { get; protected set; }

        /// <summary>
        /// The endpoint of the sender to connect to.
        /// <summary>
        public IPEndPoint Sender { get; protected set; } = new IPEndPoint(IPAddress.None, 0);

        /// <summary>
        /// Creates a new <see cref="SetupConnectionServerMessage"/> with the specified <paramref name="clientId"/>, and <paramref name="sender"/>.
        /// </summary>
        /// <param name="clientId">
        /// The Id assigned to the client.
        /// </param>
        /// <param name="sender">
        /// The endpoint of the sender to connect to.
        /// </param>
        public static SetupConnectionServerMessage Create(int clientId, IPEndPoint sender)
        {
            var message = new SetupConnectionServerMessage
            {
                ClientId = clientId,
                Sender = sender
            };

            message.Payload = message.GetPayload();

            return message;
        }

        /// <summary>
        /// Creates a new <see cref="SetupConnectionServerMessage"/> with values from the specified <paramref name="payload"/>.
        /// </summary>
        /// <param name="payload">The payload to create the message from.</param>
        /// <returns>
        /// A new <see cref="SetupConnectionServerMessage"/> if the <paramref name="payload"/> is valid; otherwise, null.
        /// </returns>
        public static SetupConnectionServerMessage? CreateFromPayload(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload)) { return null; }

            var parts = payload.Split(Separator);
            if (parts.Length != 2) { return null; }
            if (!int.TryParse(parts[0], out var clientId)) { return null; }
            if (!IPEndPoint.TryParse(parts[1], out var sender)) { return null; }

            return new SetupConnectionServerMessage
            {
                ClientId = clientId,
                Sender = sender,
                Payload = payload
            };
        }

        public override string GetPayload() => $"{ClientId}{Separator}{Sender}";

        private const string Separator = "|";
    }

    /// <summary>
    /// Represents a message sent by a TCP server when a trivia round starts.
    /// </summary>
    public class TriviaRoundStartServerMessage : TcpServerMessage
    {
        public override TcpServerMessageType MessageType => TcpServerMessageType.RoundStart;

        /// <summary>
        /// The Id assigned to the round.
        /// </summary>
        public int RoundId { get; protected set; }

        /// <summary>
        /// The number of questions in the round.
        /// </summary>
        public int QuestionCount { get; protected set; }

        /// <summary>
        /// The number of participants in the round.
        /// </summary>
        public int ParticipantCount { get; protected set; }

        /// <summary>
        /// Creates a new <see cref="TriviaRoundStartServerMessage"/> with the specified <paramref name="roundId"/>, <paramref name="questionCount"/>, and <paramref name="participantCount"/>.
        /// </summary>
        /// <param name="roundId">The Id assigned to the round.</param>
        /// <param name="questionCount">The number of questions in the round.</param>
        /// <param name="participantCount">The number of participants in the round.</param>
        public static TriviaRoundStartServerMessage Create(int roundId, int questionCount, int participantCount)
        {
            var message = new TriviaRoundStartServerMessage
            {
                RoundId = roundId,
                QuestionCount = questionCount,
                ParticipantCount = participantCount
            };

            message.Payload = message.GetPayload();

            return message;
        }

        /// <summary>
        /// Creates a new <see cref="TriviaRoundStartServerMessage"/> with values from the specified <paramref name="payload"/>.
        /// </summary>
        /// <param name="payload">The payload to create the message from.</param>
        /// <returns>
        /// A new <see cref="TriviaRoundStartServerMessage"/> if the <paramref name="payload"/> is valid; otherwise, null.
        /// </returns>
        public static TriviaRoundStartServerMessage? CreateFromPayload(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload)) { return null; }

            var parts = payload.Split(Separator);
            if (parts.Length != 3) { return null; }
            if (!int.TryParse(parts[0], out var roundId)) { return null; }
            if (!int.TryParse(parts[1], out var questionCount)) { return null; }
            if (!int.TryParse(parts[2], out var participantCount)) { return null; }

            return new TriviaRoundStartServerMessage
            {
                RoundId = roundId,
                QuestionCount = questionCount,
                ParticipantCount = participantCount,
                Payload = payload
            };
        }

        public override string GetPayload() => $"{RoundId}{Separator}{QuestionCount}{Separator}{ParticipantCount}";

        private const string Separator = "|";
    }

    /// <summary>
    /// Represents a message sent by a TCP server when a trivia question is sent.
    /// </summary>
    public class TriviaQuestionServerMessage : TcpServerMessage
    {
        public override TcpServerMessageType MessageType => TcpServerMessageType.Question;

        /// <summary>
        /// The Id assigned to the round.
        /// </summary>
        public int RoundId { get; protected set; }

        /// <summary>
        /// The number of questions in the round.
        /// </summary>
        public int QuestionCount { get; protected set; }

        /// <summary>
        /// The category of the question.
        /// </summary>
        public string Category { get; protected set; } = string.Empty;

        /// <summary>
        /// The difficulty of the question.
        /// </summary>
        public string Difficulty { get; protected set; } = string.Empty;

        /// <summary>
        /// The Id assigned to the question.
        /// </summary>
        public int QuestionId { get; protected set; }

        /// <summary>
        /// The question text.
        /// </summary>
        public string Question { get; protected set; } = string.Empty;

        /// <summary>
        /// The answers to the question.
        /// </summary>
        public IReadOnlyDictionary<int, string> Answers { get; protected set; } = new Dictionary<int, string>();

        /// <summary>
        /// Creates a new <see cref="TriviaQuestionServerMessage"/> with the specified <paramref name="roundId"/>, <paramref name="questionCount"/>, <paramref name="category"/>, <paramref name="difficulty"/>, <paramref name="questionId"/>, <paramref name="question"/>, and <paramref name="answers"/>.
        /// </summary>
        /// <param name="roundId">The Id assigned to the round.</param>
        /// <param name="questionCount">The number of questions in the round.</param>
        /// <param name="category">The category of the question.</param>
        /// <param name="difficulty">The difficulty of the question.</param>
        /// <param name="questionId">The Id assigned to the question.</param>
        /// <param name="question">The question text.</param>
        /// <param name="answers">The answers to the question.</param>
        public static TriviaQuestionServerMessage Create(int roundId, int questionCount, string category, string difficulty, int questionId, string question, IReadOnlyDictionary<int, string> answers)
        {
            var message = new TriviaQuestionServerMessage
            {
                RoundId = roundId,
                QuestionCount = questionCount,
                Category = category,
                Difficulty = difficulty,
                QuestionId = questionId,
                Question = question,
                Answers = answers
            };

            message.Payload = message.GetPayload();

            return message;
        }

        /// <summary>
        /// Creates a new <see cref="TriviaQuestionServerMessage"/> with values from the specified <paramref name="payload"/>.
        /// </summary>
        /// <param name="payload">The payload to create the message from.</param>
        /// <returns>
        /// A new <see cref="TriviaQuestionServerMessage"/> if the <paramref name="payload"/> is valid; otherwise, null.
        /// </returns>
        public static TriviaQuestionServerMessage? CreateFromPayload(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload)) { return null; }

            var parts = payload.Split(Separator);
            if (parts.Length != 7) { return null; }
            if (!int.TryParse(parts[0], out var roundId)) { return null; }
            if (!int.TryParse(parts[1], out var questionCount)) { return null; }
            if (string.IsNullOrWhiteSpace(parts[2])) { return null; }
            if (string.IsNullOrWhiteSpace(parts[3])) { return null; }
            if (!int.TryParse(parts[4], out var questionId)) { return null; }
            if (string.IsNullOrWhiteSpace(parts[5])) { return null; }
            if (string.IsNullOrWhiteSpace(parts[6])) { return null; }

            var category = parts[2].Base64Decode();
            var difficulty = parts[3].Base64Decode();
            var question = parts[5].Base64Decode();

            var answersPayload = parts[6].Split(AnswerSeparator);
            IReadOnlyDictionary<int, string> answers;
            try
            {
                answers = answersPayload.Select(a => a.Split(AnswerValueSeparator)).ToDictionary(a => int.Parse(a[0]), a => a[1].Base64Decode());
            }
            catch
            {
                return null;
            }

            return new TriviaQuestionServerMessage
            {
                RoundId = roundId,
                QuestionCount = questionCount,
                Category = category,
                Difficulty = difficulty,
                QuestionId = questionId,
                Question = question,
                Answers = answers,
                Payload = payload
            };
        }

        public override string GetPayload()
        {
            var categoryPayload = Category.Base64Encode();
            var difficultyPayload = Difficulty.Base64Encode();
            var questionPayload = Question.Base64Encode();
            var answerPayload = string.Join(AnswerSeparator, Answers.Select(a => $"{a.Key}{AnswerValueSeparator}{a.Value.Base64Encode()}"));

            var payload =  $"{RoundId}{Separator}{QuestionCount}{Separator}{categoryPayload}{Separator}{difficultyPayload}{Separator}{QuestionId}{Separator}{questionPayload}{Separator}{answerPayload}";
            return payload;
        }

        private const string Separator = "|";
        private const string AnswerSeparator = "?";
        private const string AnswerValueSeparator = ":";
    }

    /// <summary>
    /// Represents a message sent by a TCP server when a trivia question result is sent.
    /// </summary>
    public class TriviaQuestionResultServerMessage : TcpServerMessage
    {
        public override TcpServerMessageType MessageType => TcpServerMessageType.Result;

        /// <summary>
        /// The Id assigned to the round.
        /// </summary>
        public int RoundId { get; protected set; }

        /// <summary>
        /// The Id assigned to the question.
        /// </summary>
        public int QuestionId { get; protected set; }

        /// <summary>
        /// The number of questions in the round.
        /// </summary>
        public int QuestionCount { get; protected set; }

        /// <summary>
        /// The answer to the question.
        /// </summary>
        public string Answer { get; protected set; } = string.Empty;

        /// <summary>
        /// The result of the answer.
        /// </summary>
        public bool Correct { get; protected set; }

        /// <summary>
        /// The correct answer to the question.
        /// </summary>
        public string CorrectAnswer { get; protected set; } = string.Empty;

        /// <summary>
        /// Creates a new <see cref="TriviaQuestionResultServerMessage"/> with the specified <paramref name="roundId"/>, <paramref name="questionCount"/>, <paramref name="questionId"/>, <paramref name="answer"/>, <paramref name="correct"/>, and , <paramref name="correctAnswer"/>.
        /// </summary>
        /// <param name="roundId">The Id assigned to the round.</param>
        /// <param name="questionCount">The number of questions in the round.</param>
        /// <param name="questionId">The Id assigned to the question.</param>
        /// <param name="answer">The answer to the question.</param>
        /// <param name="correct">The result of the answer.</param>
        /// <param name="correctAnswer">The correct answer to the question.</param>
        public static TriviaQuestionResultServerMessage Create(int roundId, int questionCount, int questionId, string answer, bool correct, string correctAnswer)
        {
            var message = new TriviaQuestionResultServerMessage
            {
                RoundId = roundId,
                QuestionCount = questionCount,
                QuestionId = questionId,
                Answer = answer,
                Correct = correct,
                CorrectAnswer = correctAnswer
            };

            message.Payload = message.GetPayload();

            return message;
        }

        /// <summary>
        /// Creates a new <see cref="TriviaQuestionResultServerMessage"/> with values from the specified <paramref name="payload"/>.
        /// </summary>
        /// <param name="payload">The payload to create the message from.</param>
        /// <returns>
        /// A new <see cref="TriviaQuestionResultServerMessage"/> if the <paramref name="payload"/> is valid; otherwise null.
        /// </returns>
        public static TriviaQuestionResultServerMessage? CreateFromPayload(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload)) { return null; }

            var parts = payload.Split(Separator);
            if (parts.Length != 6) { return null; }
            if (!int.TryParse(parts[0], out var roundId)) { return null; }
            if (!int.TryParse(parts[1], out var questionCount)) { return null; }
            if (!int.TryParse(parts[2], out var questionId)) { return null; }
            if (string.IsNullOrWhiteSpace(parts[3])) { return null; }
            if (!bool.TryParse(parts[4], out var correct)) { return null; }
            if (string.IsNullOrWhiteSpace(parts[5])) { return null; }

            return new TriviaQuestionResultServerMessage
            {
                RoundId = roundId,
                QuestionCount = questionCount,
                QuestionId = questionId,
                Answer = parts[3].Base64Decode(),
                Correct = correct,
                CorrectAnswer = parts[5].Base64Decode(),
                Payload = payload
            };
        }

        public override string GetPayload() => $"{RoundId}{Separator}{QuestionCount}{Separator}{QuestionId}{Separator}{Answer.Base64Encode()}{Separator}{Correct}{Separator}{CorrectAnswer.Base64Encode()}";

        private const string Separator = "|";
    }

    /// <summary>
    /// Represents a message sent by a TCP server when a trivia round ends.
    /// </summary>
    public class TriviaRoundEndServerMessage : TcpServerMessage
    {
        public override TcpServerMessageType MessageType => TcpServerMessageType.RoundEnd;

        /// <summary>
        /// The Id assigned to the round.
        /// </summary>
        public int RoundId { get; protected set; }

        /// <summary>
        /// The name of the participant with the highest overall score.
        /// </summary>
        public string OverallLeader { get; protected set; } = string.Empty;

        /// <summary>
        /// The score of the participant with the highest overall score.
        /// </summary>
        public int OverallLeaderScore { get; protected set; }

        /// <summary>
        /// The name of the participant with the highest score for the round.
        /// </summary>
        public string RoundLeader { get; protected set; } = string.Empty;

        /// <summary>
        /// The score of the participant with the highest score for the round.
        /// </summary>
        public int RoundLeaderScore { get; protected set; }

        /// <summary>
        /// The overall score.
        /// </summary>
        public int OverallScore { get; protected set; }

        /// <summary>
        /// The score for the round.
        /// </summary>
        public int Score { get; protected set; }

        /// <summary>
        /// The overall rank.
        /// </summary>
        public int OverallRank { get; protected set; }

        /// <summary>
        /// The rank for the round.
        /// </summary>
        public int Rank { get; protected set; }

        /// <summary>
        /// Creates a new <see cref="TriviaRoundEndServerMessage"/> with the specified <paramref name="roundId"/>, <paramref name="overallLeader"/>, <paramref name="overallLeaderScore"/>, <paramref name="roundLeader"/>, <paramref name="roundLeaderScore"/>, <paramref name="overallScore"/>, <paramref name="score"/>, <paramref name="overallRank"/>, and <paramref name="rank"/>.
        /// </summary>
        /// <param name="roundId">The Id assigned to the round.</param>
        /// <param name="overallLeader">The name of the participant with the highest overall score.</param>
        /// <param name="overallLeaderScore">The score of the participant with the highest overall score.</param>
        /// <param name="roundLeader">The name of the participant with the highest score for the round.</param>
        /// <param name="roundLeaderScore">The score of the participant with the highest score for the round.</param>
        /// <param name="overallScore">The overall score.</param>
        /// <param name="score">The score for the round.</param>
        /// <param name="overallRank">The overall rank.</param>
        /// <param name="rank">The rank for the round.</param>
        public static TriviaRoundEndServerMessage Create(int roundId, string overallLeader, int overallLeaderScore, string roundLeader, int roundLeaderScore, int overallScore, int score, int overallRank, int rank)
        {
            var message = new TriviaRoundEndServerMessage
            {
                RoundId = roundId,
                OverallLeader = overallLeader,
                OverallLeaderScore = overallLeaderScore,
                RoundLeader = roundLeader,
                RoundLeaderScore = roundLeaderScore,
                OverallScore = overallScore,
                Score = score,
                OverallRank = overallRank,
                Rank = rank
            };

            message.Payload = message.GetPayload();

            return message;
        }

        /// <summary>
        /// Creates a new <see cref="TriviaRoundEndServerMessage"/> with values from the specified <paramref name="payload"/>.
        /// </summary>
        /// <param name="payload">The payload to create the message from.</param>
        /// <returns>
        /// A new <see cref="TriviaRoundEndServerMessage"/> if the <paramref name="payload"/> is valid; otherwise, null.
        /// </returns>
        public static TriviaRoundEndServerMessage? CreateFromPayload(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload)) { return null; }

            var parts = payload.Split(Separator);
            if (parts.Length != 9) { return null; }
            if (!int.TryParse(parts[0], out var roundId)) { return null; }
            if (string.IsNullOrWhiteSpace(parts[1])) { return null; }
            if (!int.TryParse(parts[2], out var overallLeaderScore)) { return null; }
            if (string.IsNullOrWhiteSpace(parts[3])) { return null; }
            if (!int.TryParse(parts[4], out var roundLeaderScore)) { return null; }
            if (!int.TryParse(parts[5], out var overallScore)) { return null; }
            if (!int.TryParse(parts[6], out var score)) { return null; }
            if (!int.TryParse(parts[7], out var overallRank)) { return null; }
            if (!int.TryParse(parts[8], out var rank)) { return null; }

            return new TriviaRoundEndServerMessage
            {
                RoundId = roundId,
                OverallLeader = parts[1].Base64Decode(),
                OverallLeaderScore = overallLeaderScore,
                RoundLeader = parts[3].Base64Decode(),
                RoundLeaderScore = roundLeaderScore,
                OverallScore = overallScore,
                Score = score,
                OverallRank = overallRank,
                Rank = rank,
                Payload = payload
            };
        }

        public override string GetPayload() => $"{RoundId}{Separator}{OverallLeader.Base64Encode()}{Separator}{OverallLeaderScore}{Separator}{RoundLeader.Base64Encode()}{Separator}{RoundLeaderScore}{Separator}{OverallScore}{Separator}{Score}{Separator}{OverallRank}{Separator}{Rank}";

        private const string Separator = "|";
    }

    /// <summary>
    /// Represents a message sent by a TCP server providing the round details.
    /// </summary>
    public class TriviaRoundDetailsServerMessage : TcpServerMessage
    {
        public override TcpServerMessageType MessageType => TcpServerMessageType.RoundDetails;

        /// <summary>
        /// The Id assigned to the round.
        /// </summary>
        public int RoundId { get; protected set; }

        /// <summary>
        /// The number of questions in the round.
        /// </summary>
        public int QuestionCount { get; protected set; }

        /// <summary>
        /// The number of participants in the round.
        /// </summary>
        public int ParticipantCount { get; protected set; }

        /// <summary>
        /// The category of the question.
        /// </summary>
        public string Category { get; protected set; } = string.Empty;

        /// <summary>
        /// The difficulty of the question.
        /// </summary>
        public string Difficulty { get; protected set; } = string.Empty;

        /// <summary>
        /// The Id assigned to the question.
        /// </summary>
        public int QuestionId { get; protected set; }

        /// <summary>
        /// The status of the round.
        /// </summary>
        public string Status { get; protected set; } = string.Empty;

        /// <summary>
        /// Indicates if the client is a participant in the round.
        /// </summary>
        public bool IsParticipant { get; protected set; }

        /// <summary>
        /// Creates a new <see cref="TriviaRoundDetailsServerMessage"/> with the specified <paramref name="roundId"/>, <paramref name="questionCount"/>, <paramref name="participantCount"/>, <paramref name="category"/>, <paramref name="difficulty"/>, <paramref name="questionId"/>, <paramref name="status"/>, and <paramref name="isParticipant"/>.
        /// </summary>
        /// <param name="roundId">The Id assigned to the round.</param>
        /// <param name="questionCount">The number of questions in the round.</param>
        /// <param name="participantCount">The number of participants in the round.</param>
        /// <param name="category">The category of the question.</param>
        /// <param name="difficulty">The difficulty of the question.</param>
        /// <param name="questionId">The Id assigned to the question.</param>
        /// <param name="status">The status of the round.</param>
        /// <param name="isParticipant">Indicates if the client is a participant in the round.</param>
        public static TriviaRoundDetailsServerMessage Create(int roundId, int questionCount, int participantCount, string category, string difficulty, int questionId, string status, bool isParticipant)
        {
            var message = new TriviaRoundDetailsServerMessage
            {
                RoundId = roundId,
                QuestionCount = questionCount,
                ParticipantCount = participantCount,
                Category = category,
                Difficulty = difficulty,
                QuestionId = questionId,
                Status = status,
                IsParticipant = isParticipant
            };

            message.Payload = message.GetPayload();

            return message;
        }

        /// <summary>
        /// Creates a new <see cref="TriviaRoundDetailsServerMessage"/> with values from the specified <paramref name="payload"/>.
        /// </summary>
        /// <param name="payload">
        /// The payload to create the message from.
        /// </param>
        /// <returns>
        /// A new <see cref="TriviaRoundDetailsServerMessage"/> if the <paramref name="payload"/> is valid; otherwise, null.
        /// </returns>
        public static TriviaRoundDetailsServerMessage? CreateFromPayload(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload)) { return null; }

            var parts = payload.Split(Separator);
            if (parts.Length != 8) { return null; }
            if (!int.TryParse(parts[0], out var roundId)) { return null; }
            if (!int.TryParse(parts[1], out var questionCount)) { return null; }
            if (!int.TryParse(parts[2], out var participantCount)) { return null; }
            if (string.IsNullOrWhiteSpace(parts[3])) { return null; }
            if (string.IsNullOrWhiteSpace(parts[4])) { return null; }
            if (!int.TryParse(parts[5], out var questionId)) { return null; }
            if (string.IsNullOrWhiteSpace(parts[6])) { return null; }
            if (!bool.TryParse(parts[7], out var isParticipant)) { return null; }

            return new TriviaRoundDetailsServerMessage
            {
                RoundId = roundId,
                QuestionCount = questionCount,
                ParticipantCount = participantCount,
                Category = parts[3].Base64Decode(),
                Difficulty = parts[4].Base64Decode(),
                QuestionId = questionId,
                Status = parts[6].Base64Decode(),
                IsParticipant = isParticipant,
                Payload = payload
            };
        }

        public override string GetPayload() => $"{RoundId}{Separator}{QuestionCount}{Separator}{ParticipantCount}{Separator}{Category.Base64Encode()}{Separator}{Difficulty.Base64Encode()}{Separator}{QuestionId}{Separator}{Status.Base64Encode()}{Separator}{IsParticipant}";

        private const string Separator = "|";
    }

    /// <summary>
    /// Represents a message sent by a TCP server when the messages has been accepted.
    /// </summary>
    public class AcceptedServerMessage : TcpServerMessage
    {
        public override TcpServerMessageType MessageType => TcpServerMessageType.Accepted;

        /// <summary>
        /// Creates a new <see cref="AcceptedServerMessage"/>/>.
        /// </summary>
        public static AcceptedServerMessage Create() => new();
    }

    /// <summary>
    /// Represents a message sent by a TCP server when an error occurs.
    /// </summary>
    public class ErrorServerMessage : TcpServerMessage
    {
        public override TcpServerMessageType MessageType => TcpServerMessageType.Error;

        /// <summary>
        /// The error message.
        /// </summary>
        public string ErrorMessage { get; protected set; } = string.Empty;

        /// <summary>
        /// Creates a new <see cref="ErrorServerMessage"/> with the specified <paramref name="errorMessage"/>.
        /// </summary>
        /// <param name="errorMessage">
        /// The error message.
        /// </param>
        public static ErrorServerMessage Create(string errorMessage)
        {
            return new ErrorServerMessage
            {
                ErrorMessage = errorMessage,
                Payload = errorMessage
            };
        }

        /// <summary>
        /// Creates a new <see cref="ErrorServerMessage"/> with values from the specified <paramref name="payload"/>.
        /// </summary>
        /// <param name="payload">The payload to create the message from.</param>
        /// <returns>
        /// A new <see cref="ErrorServerMessage"/> if the <paramref name="payload"/> is valid; otherwise, null.
        /// </returns>
        public static ErrorServerMessage? CreateFromPayload(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload)) { return null; }

            return new ErrorServerMessage
            {
                ErrorMessage = payload.Base64Decode(),
                Payload = payload
            };
        }

        public override string GetPayload() => ErrorMessage.Base64Encode();
    }
}