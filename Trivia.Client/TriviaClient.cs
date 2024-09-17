using System.Net;
using System.Net.Sockets;
using Trivia.Common;

namespace Trivia.Client
{
    internal class RoundStartEventArgs : EventArgs
    {
        /// <summary>
        /// The Id assigned to the round.
        /// </summary>
        public int RoundId { get; set; }

        /// <summary>
        /// The total number of questions in the round.
        /// </summary>
        public int QuestionCount { get; set; }

        /// <summary>
        /// The total number of participants in the round.
        /// </summary>
        public int ParticipantCount { get; set; }
    }

    internal class NewQuestionEventArgs : EventArgs
    {
        /// <summary>
        /// The Id assigned to the round.
        /// </summary>
        public int RoundId { get; set; }

        /// <summary>
        /// The total number of questions in the round.
        /// </summary>
        public int QuestionCount { get; set; }

        /// <summary>
        /// The category of the question.
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// The difficulty of the question.
        /// </summary>
        public string Difficulty { get; set; } = string.Empty;

        /// <summary>
        /// The Id assigned to the question.
        /// </summary>
        public int QuestionId { get; set; }

        /// <summary>
        /// The question text.
        /// </summary>
        public string Question { get; set; } = string.Empty;

        /// <summary>
        /// The answers to the question.
        /// </summary>
        public IReadOnlyDictionary<int, string> Answers { get; set; } = new Dictionary<int, string>();
    }

    internal class QuestionResultEventArgs : EventArgs
    {
        /// <summary>
        /// The Id assigned to the round.
        /// </summary>
        public int RoundId { get; set; }

        /// <summary>
        /// The Id assigned to the question.
        /// </summary>
        public int QuestionId { get; set; }

        /// <summary>
        /// The number of questions in the round.
        /// </summary>
        public int QuestionCount { get; set; }

        /// <summary>
        /// The answer to the question.
        /// </summary>
        public string Answer { get; set; } = string.Empty;

        /// <summary>
        /// The result of the answer.
        /// </summary>
        public bool Correct { get; set; }

        /// <summary>
        /// The correct answer to the question.
        /// </summary>
        public string CorrectAnswer { get; set; } = string.Empty;
    }

    internal class RoundEndEventArgs : EventArgs
    {
        /// <summary>
        /// The Id assigned to the round.
        /// </summary>
        public int RoundId { get; set; }

        /// <summary>
        /// The name of the participant with the highest overall score.
        /// </summary>
        public string OverallLeader { get; set; } = string.Empty;

        /// <summary>
        /// The score of the participant with the highest overall score.
        /// </summary>
        public int OverallLeaderScore { get; set; }

        /// <summary>
        /// The name of the participant with the highest score for the round.
        /// </summary>
        public string RoundLeader { get; set; } = string.Empty;

        /// <summary>
        /// The score of the participant with the highest score for the round.
        /// </summary>
        public int RoundLeaderScore { get; set; }

        /// <summary>
        /// The overall score.
        /// </summary>
        public int OverallScore { get; set; }

        /// <summary>
        /// The score for the round.
        /// </summary>
        public int Score { get; set; }

        /// <summary>
        /// The overall rank.
        /// </summary>
        public int OverallRank { get; set; }

        /// <summary>
        /// The rank for the round.
        /// </summary>
        public int Rank { get; set; }
    }

    internal class RoundDetailsEventArgs : EventArgs
    {
        /// <summary>
        /// The Id assigned to the round.
        /// </summary>
        public int RoundId { get; set; }

        /// <summary>
        /// The number of questions in the round.
        /// </summary>
        public int QuestionCount { get; set; }

        /// <summary>
        /// The number of participants in the round.
        /// </summary>
        public int ParticipantCount { get; set; }

        /// <summary>
        /// The category of the question.
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// The difficulty of the question.
        /// </summary>
        public string Difficulty { get; set; } = string.Empty;

        /// <summary>
        /// The Id assigned to the question.
        /// </summary>
        public int QuestionId { get; set; }

        /// <summary>
        /// The status of the round.
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Indicates if the client is a participant in the round.
        /// </summary>
        public bool IsParticipant { get; set; }
    }

    internal class ErrorEventArgs : EventArgs
    {
        /// <summary>
        /// The category of the error.
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// The error message.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// The exception that caused the error.
        /// </summary>
        public Exception? Exception { get; set; } = null;
    }

    internal class TriviaClient
    {
        private IPEndPoint? _serverEndPoint;
        private TcpClient? _receiver;

        private string _name = string.Empty;
        private int _clientId;

        private Task? _incomingMessageTask;

        /// <summary>
        /// The connection state of the client.
        /// </summary>
        public bool IsConnected => _receiver?.Connected ?? false;

        /// <summary>
        /// The state of the client receiving messages.
        /// </summary>
        public bool IsReceivingMessages => _incomingMessageTask != null
                    && _incomingMessageTask.Status != TaskStatus.Canceled
                    && _incomingMessageTask.Status != TaskStatus.Faulted
                    && _incomingMessageTask.Status != TaskStatus.RanToCompletion;

        /// <summary>
        /// Occurs when a new round starts.
        /// </summary>
        public event EventHandler<RoundStartEventArgs>? OnRoundStart;

        /// <summary>
        /// Occurs when a new question is received.
        /// </summary>
        public event EventHandler<NewQuestionEventArgs>? OnNewQuestion;

        /// <summary>
        /// Occurs when a question result is received.
        /// </summary>
        public event EventHandler<QuestionResultEventArgs>? OnQuestionResult;

        /// <summary>
        /// Occurs when an error occurred.
        /// </summary>
        public event EventHandler<ErrorEventArgs>? OnError;

        /// <summary>
        /// Occurs when a round ends.
        /// </summary>
        public event EventHandler<RoundEndEventArgs>? OnRoundEnd;

        /// <summary>
        /// Occurs when round details are received.
        /// </summary>
        public event EventHandler<RoundDetailsEventArgs>? OnRoundDetails;

        private CancellationTokenSource? _cancellationTokenSource;
        private CancellationToken _cancellationToken = CancellationToken.None;

        /// <summary>
        /// Connect to the server.
        /// </summary>
        /// <param name="serverEndPoint">The server endpoint to connect to.</param>
        /// <param name="name">The name of the client to register.</param>
        /// <returns></returns>
        public async Task<bool> ConnectAsync(IPEndPoint serverEndPoint, string name)
        {
            _serverEndPoint = serverEndPoint;
            _name = name;
            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = _cancellationTokenSource.Token;

            try
            {
                var result = await SetupConnectionAsync(_cancellationToken);
                if (!result) { return false; }

                _incomingMessageTask = Task.Run(async () => await HandleIncomingMessageAsync(_cancellationToken), _cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                _cancellationTokenSource?.Cancel();
                _receiver?.Close();

                var args = new ErrorEventArgs
                {
                    Category = "Client",
                    ErrorMessage = "Could not connect to server.",
                    Exception = ex
                };
                OnError?.Invoke(this, args);
                return false;
            }
        }

        private async Task<bool> SetupConnectionAsync(CancellationToken cancellationToken = default)
        {
            if (_serverEndPoint == null) { throw new Exception("Unknown server details."); }

            using var sender = new TcpClient();
            sender.Connect(_serverEndPoint);
            if (!sender.Connected) { throw new Exception("Could not connect to server."); }

            await sender.SendRequestConnectionToServerAsync(_name, cancellationToken);
            var setupResponse = await sender.ReadSetupConnectionFromServerAsync(true, cancellationToken: cancellationToken);
            if (setupResponse == null) { throw new Exception("Could not setup connection to server."); }

            _clientId = setupResponse.ClientId;
            _receiver = new TcpClient();
            _receiver.Connect(setupResponse.Sender);

            await _receiver.SendEstablishConnectionToServerAsync(_clientId, _name, cancellationToken);
            var response = await _receiver.ReadFromServerAsync(true, cancellationToken: cancellationToken);
            return response?.MessageType == TcpServerMessageType.Accepted;
        }

        private async Task HandleIncomingMessageAsync(CancellationToken cancellationToken = default)
        {
            while (true)
            {
                if (cancellationToken.IsCancellationRequested) { return; }
                if (_receiver == null) { return; }

                if (_receiver.Available == 0) { continue; }

                var message = await _receiver.ReadFromServerAsync(cancellationToken: cancellationToken)
                    ?? throw new Exception("Could not read message from server.");

                HandleTcpServerMessage(message);
            }
        }

        private void HandleTcpServerMessage(TcpServerMessage message)
        {
            switch (message.MessageType)
            {
                case TcpServerMessageType.RoundStart:
                    HandleRoundStart(message.ToTriviaRoundStart());
                    break;

                case TcpServerMessageType.Question:
                    HandleNewQuestion(message.ToTriviaQuestion());
                    break;

                case TcpServerMessageType.Result:
                    HandleQuestionResult(message.ToTriviaQuestionResult());
                    break;

                case TcpServerMessageType.RoundEnd:
                    HandleRoundEnd(message.ToTriviaRoundEnd());
                    break;

                case TcpServerMessageType.RoundDetails:
                    HandleRoundDetails(message.ToTriviaRoundDetails());
                    break;

                case TcpServerMessageType.Error:
                    HandleError(message.ToError());
                    break;
            }
        }

        private void HandleRoundStart(TriviaRoundStartServerMessage? message)
        {
            if (message == null)
            {
                var error = new ErrorEventArgs
                {
                    Category = "Server",
                    ErrorMessage = "Invalid payload"
                };

                OnError?.Invoke(this, error);
                return;
            }

            var args = new RoundStartEventArgs
            {
                RoundId = message.RoundId,
                QuestionCount = message.QuestionCount,
                ParticipantCount = message.ParticipantCount
            };

            OnRoundStart?.Invoke(this, args);
        }

        private void HandleNewQuestion(TriviaQuestionServerMessage? message)
        {
            if (message == null)
            {
                var error = new ErrorEventArgs
                {
                    Category = "Server",
                    ErrorMessage = "Invalid payload"
                };

                OnError?.Invoke(this, error);
                return;
            }

            var args = new NewQuestionEventArgs
            {
                RoundId = message.RoundId,
                QuestionCount = message.QuestionCount,
                Category = message.Category,
                Difficulty = message.Difficulty,
                QuestionId = message.QuestionId,
                Question = message.Question,
                Answers = message.Answers
            };

            OnNewQuestion?.Invoke(this, args);
        }

        private void HandleQuestionResult(TriviaQuestionResultServerMessage? message)
        {
            if (message == null)
            {
                var error = new ErrorEventArgs
                {
                    Category = "Server",
                    ErrorMessage = "Invalid payload"
                };

                OnError?.Invoke(this, error);
                return;
            }

            var args = new QuestionResultEventArgs
            {
                RoundId = message.RoundId,
                QuestionId = message.QuestionId,
                QuestionCount = message.QuestionCount,
                Answer = message.Answer,
                Correct = message.Correct,
                CorrectAnswer = message.CorrectAnswer
            };

            OnQuestionResult?.Invoke(this, args);
        }

        private void HandleRoundEnd(TriviaRoundEndServerMessage? message)
        {
            if (message == null)
            {
                var error = new ErrorEventArgs
                {
                    Category = "Server",
                    ErrorMessage = "Invalid payload"
                };

                OnError?.Invoke(this, error);
                return;
            }

            var args = new RoundEndEventArgs
            {
                RoundId = message.RoundId,
                OverallLeader = message.OverallLeader,
                OverallLeaderScore = message.OverallLeaderScore,
                RoundLeader = message.RoundLeader,
                RoundLeaderScore = message.RoundLeaderScore,
                OverallScore = message.OverallScore,
                Score = message.Score,
                OverallRank = message.OverallRank,
                Rank = message.Rank
            };

            OnRoundEnd?.Invoke(this, args);
        }

        private void HandleRoundDetails(TriviaRoundDetailsServerMessage? message)
        {
            if (message == null)
            {
                var error = new ErrorEventArgs
                {
                    Category = "Server",
                    ErrorMessage = "Invalid payload"
                };

                OnError?.Invoke(this, error);
                return;
            }

            var args = new RoundDetailsEventArgs
            {
                Category = message.Category,
                Difficulty = message.Difficulty,
                IsParticipant = message.IsParticipant,
                ParticipantCount = message.ParticipantCount,
                QuestionCount = message.QuestionCount,
                QuestionId = message.QuestionId,
                RoundId = message.RoundId,
                Status = message.Status
            };

            OnRoundDetails?.Invoke(this, args);
        }

        private void HandleError(ErrorServerMessage? message)
        {
            if (message == null)
            {
                var error = new ErrorEventArgs
                {
                    Category = "Server",
                    ErrorMessage = "Invalid payload"
                };

                OnError?.Invoke(this, error);
                return;
            }

            var args = new ErrorEventArgs
            {
                Category = "Server",
                ErrorMessage = message.ErrorMessage,
                Exception = new Exception(message.ErrorMessage)
            };

            OnError?.Invoke(this, args);
        }

        /// <summary>
        /// Send an answer to the server.
        /// </summary>
        public async Task<bool> SendAnswerAsync(int roundId, int questionId, int answerId)
        {
            if (!IsConnected) { return false; }

            var response = await _serverEndPoint!.SendAnswerToServerAsync(roundId, questionId, _clientId, _name, answerId, _cancellationToken);
            if (response.MessageType == TcpServerMessageType.Error)
            {
                var error = response.ToError();

                var args = new ErrorEventArgs
                {
                    Category = "Server",
                    ErrorMessage = error?.ErrorMessage ?? "Error"
                };
                OnError?.Invoke(this, args);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Send a request for round details to the server.
        /// </summary>
        public async Task<bool> SendRequestRoundDetailsAsync()
        {
            if (!IsConnected) { return false; }

            var response = await _serverEndPoint!.SendRequestRoundDetailsToServerAsync(_clientId, _name, _cancellationToken);
            if (response.MessageType == TcpServerMessageType.Error)
            {
                var error = response.ToError();

                var args = new ErrorEventArgs
                {
                    Category = "Server",
                    ErrorMessage = error?.ErrorMessage ?? "Error"
                };
                OnError?.Invoke(this, args);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Disconnect from the server.
        /// </summary>
        public async Task Disconnect()
        {
            if (!IsConnected) { return; }

            var response = await _serverEndPoint!.SendDisconnectToServerAsync(_clientId, _name, _cancellationToken);
            if (response.MessageType == TcpServerMessageType.Error)
            {
                var error = response.ToError();
                var args = new ErrorEventArgs
                {
                    Category = "Server",
                    ErrorMessage = error?.ErrorMessage ?? "Error"
                };
                OnError?.Invoke(this, args);
            }

            _cancellationTokenSource?.Cancel();
            _receiver?.Close();
        }
    }
}