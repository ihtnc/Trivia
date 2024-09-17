using Trivia.Common;
using Trivia.Server.TriviaGame;

namespace Trivia.Server
{
    public enum TriviaState
    {
        NotStarted,
        WaitingForRoundToStart,
        SendQuestion,
        QuestionSent,
        WaitingForAnswers,
        EvaluateAnswers,
        WaitingForNextQuestion,
        RoundComplete,
        WaitingForNextRound
    }

    /// <summary>
    /// Represents the server for the trivia game.
    /// </summary>
    internal class GameServer(ITriviaProvider provider, ITriviaRound triviaRound) : BaseServer
    {
        /// <summary>
        /// The Id assigned to the next client.
        /// </summary>
        private int _roundId = 0;

        /// <summary>
        /// The current round in the trivia game.
        /// </summary>
        public ITriviaRound Round { get; } = triviaRound;

        private Task _gameTask = Task.CompletedTask;

        public override event EventHandler<ServerLogEventArgs>? OnServerLog;
        public override event EventHandler<ServerErrorEventArgs>? OnServerError;
        public override event EventHandler<NewServerActionEventArgs>? OnNewServerAction;

        /// <summary>
        /// The state of the game.
        /// </summary>
        public TriviaState State { get; private set; } = TriviaState.NotStarted;

        /// <summary>
        /// Moves the game to the next state.
        /// </summary>
        public void MoveToNextState()
        {
            State = State switch
            {
                TriviaState.NotStarted => TriviaState.WaitingForRoundToStart,
                TriviaState.WaitingForRoundToStart => TriviaState.SendQuestion,
                TriviaState.SendQuestion => TriviaState.QuestionSent,
                TriviaState.QuestionSent => TriviaState.WaitingForAnswers,
                TriviaState.WaitingForAnswers => TriviaState.EvaluateAnswers,
                TriviaState.EvaluateAnswers => TriviaState.WaitingForNextQuestion,
                TriviaState.WaitingForNextQuestion => TriviaState.SendQuestion,
                TriviaState.RoundComplete => TriviaState.WaitingForNextRound,
                _ => TriviaState.WaitingForRoundToStart,
            };
        }

        /// <summary>
        /// Moves the game to the end state.
        /// </summary>
        public void MoveToEndState()
        {
            State = TriviaState.RoundComplete;
        }

        /// <summary>
        /// Resets the state of the game.
        /// </summary>
        public void ResetState()
        {
            State = TriviaState.WaitingForRoundToStart;
        }

        /// <summary>
        /// The delay in milliseconds before starting a new round.
        /// </summary>
        public int RoundStartDelay { get; private set; } = 30 * 1000;

        /// <summary>
        /// Sets the delay in milliseconds before starting a new round.
        /// </summary>
        /// <param name="delay">The delay in milliseconds.</param>
        /// <returns>
        /// <see langword="true"/> if the delay was set; otherwise, <see langword="false"/>.
        /// </returns>
        public bool SetRoundStartDelay(int delay)
        {
            if (delay < 0) { return false; }

            RoundStartDelay = delay;
            return true;
        }

        /// <summary>
        /// The delay in milliseconds before evaluating answers.
        /// </summary>
        public int EvaluateAnswerDelay { get; private set; } = 30 * 1000;

        /// <summary>
        /// Sets the delay in milliseconds before evaluating answers.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if the delay was set; otherwise, <see langword="false"/>.
        /// </returns>
        public bool SetEvaluateAnswerDelay(int delay)
        {
            if (delay < 0) { return false; }

            EvaluateAnswerDelay = delay;
            return true;
        }

        /// <summary>
        /// The delay in milliseconds before starting a new question.
        /// </summary>
        public int NextQuestionDelay { get; private set; } = 10 * 1000;

        /// <summary>
        /// Sets the delay in milliseconds before starting a new question.
        /// </summary>
        /// <param name="delay">The delay in milliseconds.</param>
        /// <returns>
        /// <see langword="true"/> if the delay was set; otherwise, <see langword="false"/>.
        /// </returns>
        public bool SetNextQuestionDelay(int delay)
        {
            if (delay < 0) { return false; }

            NextQuestionDelay = delay;
            return true;
        }

        private TriviaCategory? _selectedCategory;

        /// <summary>
        /// The category to use for the next round.
        /// </summary>
        public string Category => _selectedCategory?.Category ?? string.Empty;

        /// <summary>
        /// The method to get the category for the next round.
        /// </summary>
        /// <param name="category">The category to use for the next round.</param>
        /// <returns>
        /// <see langword="true"/> if the category was set; otherwise, <see langword="false"/>.
        /// </returns>
        public bool SetCategory(string category)
        {
            var bothBlank = string.IsNullOrWhiteSpace(category) && _selectedCategory == null;
            var selected = Categories.FirstOrDefault(c => c.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
            var notFound = !string.IsNullOrWhiteSpace(category) && selected == null;
            var sameValue = selected != null && _selectedCategory?.Id == selected.Id;

            if (bothBlank || notFound || sameValue)
            {
                OnServerLog?.Invoke(this, new ServerLogEventArgs("Option", "Category did not change."));
                return false;
            }

            _selectedCategory = selected;
            OnServerLog?.Invoke(this, new ServerLogEventArgs("Option", $"Category changed to '{(string.IsNullOrWhiteSpace(category) ? "Any" : category)}'."));
            return true;
        }

        /// <summary>
        /// The method to get the category for the next round.
        /// </summary>
        public TriviaDifficulty Difficulty { get; private set; } = TriviaDifficulty.Any;

        /// <summary>
        /// The method to get the difficulty for the next round.
        /// </summary>
        /// <param name="difficulty">The difficulty to use for the next round.</param>
        /// <returns>
        /// <see langword="true"/> if the difficulty was set; otherwise, <see langword="false"/>.
        /// </returns>
        public bool SetDifficulty(TriviaDifficulty difficulty)
        {
            Difficulty = difficulty;
            OnServerLog?.Invoke(this, new ServerLogEventArgs("Option", $"Difficulty changed to '{difficulty}'."));
            return true;
        }

        /// <summary>
        /// The number of questions to use for the next round.
        /// </summary>
        public int NumberOfQuestions { get; private set; } = 10;

        /// <summary>
        /// The method to get the number of questions for the next round.
        /// </summary>
        /// <param name="numberOfQuestions">The number of questions to use for the next round.</param>
        /// <returns>
        /// <see langword="true"/> if the number of questions was set; otherwise, <see langword="false"/>.
        /// </returns>
        public bool SetNumberOfQuestions(int numberOfQuestions)
        {
            var equal = numberOfQuestions == NumberOfQuestions;
            var lessThanMinimum = numberOfQuestions < 5;
            var greaterThanMaximum = numberOfQuestions > 15;

            if (equal || lessThanMinimum || greaterThanMaximum)
            {
                OnServerLog?.Invoke(this, new ServerLogEventArgs("Option", $"Number of questions did not change."));
                return false;
            }

            NumberOfQuestions = numberOfQuestions;

            OnServerLog?.Invoke(this, new ServerLogEventArgs("Option", $"Number of questions changed to '{numberOfQuestions}'."));

            return true;
        }

        /// <summary>
        /// The categories available for the trivia game.
        /// </summary>
        public IReadOnlyCollection<TriviaCategory> Categories { get; private set; } = [];

        private CancellationTokenSource _cts = new();
        private CancellationToken _cancellationToken = CancellationToken.None;

        /// <summary>
        /// The participants available for the trivia game.
        /// </summary>
        public IReadOnlyCollection<ITriviaParticipant> Participants => _participants.Values;
        private Dictionary<int, ITriviaParticipant> _participants { get; } = [];

        private readonly ITriviaProvider _provider = provider;

        /// <summary>
        /// Starts the server.
        /// </summary>
        public override CancellationTokenSource Start()
        {
            _cts = new CancellationTokenSource();
            _cancellationToken = _cts.Token;
            _gameTask = Task.Run(() => HandleGameAsync(_cancellationToken), _cancellationToken);

            var categoryTask = _provider.GetCategoriesAsync(_cancellationToken);
            categoryTask.Wait();
            Categories = categoryTask.Result;

            OnServerLog?.Invoke(this, new ServerLogEventArgs("Start", "Server started"));

            return _cts;
        }

        /// <summary>
        /// Stops the server.
        /// </summary>
        public override void Stop()
        {
            _cts.Cancel();
            OnServerLog?.Invoke(this, new ServerLogEventArgs("Stop", "Server stopped"));
        }

        /// <summary>
        /// Pings the server to check if it is running.
        /// </summary>
        public override bool Ping()
        {
            var tasksAreActive = _gameTask.Status != TaskStatus.Canceled
                && _gameTask.Status != TaskStatus.Faulted
                && _gameTask.Status != TaskStatus.RanToCompletion;
            if (tasksAreActive)
            {
                OnServerLog?.Invoke(this, new ServerLogEventArgs("Ping", "Tasks are running"));
            }
            else
            {
                OnServerError?.Invoke(this, new ServerErrorEventArgs("Ping", new Exception("Tasks are not running")));
            }

            return tasksAreActive;
        }

        /// <summary>
        /// Adds a participant to the trivia game.
        /// </summary>
        /// <param name="participant">The Id assigned to the client.</param>
        /// <returns>
        /// <see langword="true"/> if the participant was added; otherwise, <see langword="false"/>.
        /// </returns>
        public bool AddParticipant(ITriviaParticipant participant)
        {
            if (_participants.ContainsKey(participant.ClientId))
            {
                OnServerError?.Invoke(this, new ServerErrorEventArgs("AddParticipant", new Exception($"Client {participant.ClientId} is already a participant.")));
                return false;
            }

            _participants.Add(participant.ClientId, participant);
            OnServerLog?.Invoke(this, new ServerLogEventArgs("AddParticipant", $"Client {participant.ClientId} added as a participant."));
            return true;
        }

        /// <summary>
        /// Removes a participant from the trivia game.
        /// </summary>
        /// <param name="clientId">The Id assigned to the client.</param>
        /// <returns>
        /// <see langword="true"/> if the participant was removed; otherwise, <see langword="false"/>.
        /// </returns>
        public bool RemoveParticipant(int clientId)
        {
            if (!_participants.ContainsKey(clientId))
            {
                OnServerError?.Invoke(this, new ServerErrorEventArgs("RemoveParticipant", new Exception($"Client {clientId} is not a participant.")));
                return false;
            }

            _participants.Remove(clientId);
            OnServerLog?.Invoke(this, new ServerLogEventArgs("RemoveParticipant", $"Client {clientId} removed as a participant"));
            return true;
        }

        /// <summary>
        /// Sets the answer for a participant in the trivia game.
        /// </summary>
        /// <param name="clientId">The Id assigned to the client.</param>
        /// <param name="answer">The answer to the question.</param>
        /// <returns><see langword="true"/> if the answer was set; otherwise, <see langword="false"/>.</returns>
        public bool SetParticipantAnswer(int clientId, TriviaRoundAnswer answer)
        {
            if (!Round.IsRoundStarted || Round.IsRoundOver)
            {
                OnServerError?.Invoke(this, new ServerErrorEventArgs("SetParticipantAnswer", new Exception($"Trivia round is no longer accepting answers.")));
                return false;
            }

            if (!_participants.ContainsKey(clientId))
            {
                OnServerError?.Invoke(this, new ServerErrorEventArgs("SetParticipantAnswer", new Exception($"Client {clientId} is not a participant.")));
                return false;
            }

            if (answer.RoundId != Round.RoundId || answer.QuestionId != Round.CurrentQuestion?.QuestionId)
            {
                OnServerError?.Invoke(this, new ServerErrorEventArgs("SetParticipantAnswer", new Exception($"Client {clientId}'s answer is not for current round or question")));
                return false;
            }

            Round.AddParticipantAnswer(answer);
            return true;
        }

        /// <summary>
        /// Requests the server to provide the current details to a participant
        /// </summary>
        /// <param name="clientId">The Id assigned to the client.</param>
        /// <returns><see langword="true"/> if the round details are provided; otherwise, <see langword="false"/>.</returns>
        public bool ProvideRoundDetailsToParticipant(int clientId)
        {
            var numberOfQuestions = Round.IsRoundStarted ? Round.NumberOfQuestions : NumberOfQuestions;
            var numberOfParticipants = Round.IsRoundStarted ? Round.ParticipantClientIds.Count : _participants.Count;
            var category = Round.IsRoundStarted ? Round.Category?.Category : $"{_selectedCategory?.Category}";
            category = string.IsNullOrWhiteSpace(category) ? "Any" : category;
            var difficulty = Round.IsRoundStarted ? $"{Round.Difficulty}" : $"{Difficulty}";
            var isParticipant = Round.IsRoundStarted ? Round.ParticipantClientIds.Contains(clientId) : _participants.ContainsKey(clientId);
            var mesasge = TriviaRoundDetailsServerMessage.Create(Round.RoundId, numberOfQuestions, numberOfParticipants, category, difficulty, Round.CurrentQuestion?.QuestionId ?? 0, $"{State}", isParticipant);
            var sendRoundDetailsAction = new SendClientMessageServerAction(clientId, mesasge);
            OnNewServerAction?.Invoke(this, new NewServerActionEventArgs(sendRoundDetailsAction));
            return true;
        }

        /// <summary>
        /// Handles the trivia game.
        /// </summary>
        private async Task HandleGameAsync(CancellationToken cancellationToken = default)
        {
            while (true)
            {
                if (cancellationToken.IsCancellationRequested) { return; }

                if (State == TriviaState.WaitingForNextRound) { continue; }

                // Round can start only if there is at least 1 participants
                if (_participants.Count == 0)
                {
                    await Task.Delay(100, cancellationToken);
                    continue;
                }

                OnServerLog?.Invoke(this, new ServerLogEventArgs("Game", $"Round will start in {RoundStartDelay / 1000} second(s)"));

                // Wait a while to allow more participants to join
                ResetState();
                await Task.Delay(RoundStartDelay, cancellationToken);

                Round.Reset();

                if (_participants.Count == 0)
                {
                    OnServerLog?.Invoke(this, new ServerLogEventArgs("Game", $"Round not started due to lack of participants."));
                    continue;
                }

                OnServerLog?.Invoke(this, new ServerLogEventArgs("Game", "Round starting..."));

                await HandleRoundStart(cancellationToken);

                while(Round.IsRoundStarted)
                {
                    if (cancellationToken.IsCancellationRequested) { return; }

                    if (_participants.Count == 0)
                    {
                        ResetState();
                        Round.Stop();
                        OnServerLog?.Invoke(this, new ServerLogEventArgs("Game", $"Round terminated due to lack of participants."));
                        break;
                    }

                    if (State == TriviaState.SendQuestion)
                    {
                        HandleNewQuestion(cancellationToken);
                    }

                    if (State == TriviaState.WaitingForAnswers)
                    {
                        // Wait a while to let the participants read and answer the question
                        await Task.Delay(EvaluateAnswerDelay, cancellationToken);
                        if (_participants.Count == 0) { continue; }

                        HandleAnswerEvaluation(cancellationToken);
                    }

                    if (State == TriviaState.WaitingForNextQuestion)
                    {
                        // Wait a while to allow the participants to see the results
                        await Task.Delay(NextQuestionDelay, cancellationToken);
                        if (_participants.Count == 0) { continue; }

                        // Move to the next question or end the round
                        var hasMore = Round.NextQuestion();
                        if (hasMore)
                        {
                            MoveToNextState();
                        }
                        else
                        {
                            MoveToEndState();
                        }
                    }

                    if (State == TriviaState.RoundComplete)
                    {
                        HandleRoundEnd(cancellationToken);
                    }
                }
            }
        }

        /// <summary>
        /// Handles the start of a new round.
        /// </summary>
        private async Task HandleRoundStart(CancellationToken cancellationToken = default)
        {
            // Start the round and initialise values
            await Round.Start(++_roundId, _participants.Keys, Difficulty, _selectedCategory, NumberOfQuestions, cancellationToken);

            // Notify the participants that the round has started
            var message = TriviaRoundStartServerMessage.Create(Round.RoundId, Round.NumberOfQuestions, _participants.Count);
            NotifyParticipants(message, cancellationToken);

            var category = $"Category: {(string.IsNullOrWhiteSpace(Category) ? "Any" : Category)} ({Difficulty})";
            var questions = $"Questions: {NumberOfQuestions}";
            OnServerLog?.Invoke(this, new ServerLogEventArgs("Game", $"Round started. {category}; {questions};"));

            var nextStateAction = new MoveGameToNextStateServerAction();
            OnNewServerAction?.Invoke(this, new NewServerActionEventArgs(nextStateAction));
        }

        /// <summary>
        /// Handles the start of a new question.
        /// </summary>
        private void HandleNewQuestion(CancellationToken cancellationToken = default)
        {
            OnServerLog?.Invoke(this, new ServerLogEventArgs("Game", $"New question"));
            var question = Round.CurrentQuestion!;

            // Notify the participants of the new question
            var questionMessage = TriviaQuestionServerMessage.Create(Round.RoundId, Round.NumberOfQuestions, question.Category, question.Difficulty, question.QuestionId, question.Question, question.Options);
            NotifyParticipants(questionMessage, cancellationToken);

            OnServerLog?.Invoke(this, new ServerLogEventArgs("Game", $"Question sent"));

            MoveToNextState();
            var nextStateAction = new MoveGameToNextStateServerAction();
            OnNewServerAction?.Invoke(this, new NewServerActionEventArgs(nextStateAction));
        }

        /// <summary>
        /// Handles the evaluation of the answers.
        /// </summary>
        private void HandleAnswerEvaluation(CancellationToken cancellationToken = default)
        {
            // Evaluate the answers, add points to the clients with the correct answers, and notify the participants

            Round.EvaluateAnswers();
            var results = Round.CurrentResults;
            var question = Round.CurrentQuestion!;

            foreach (var clientId in Round.ParticipantClientIds)
            {
                if (cancellationToken.IsCancellationRequested) { return; }

                if (results[clientId] && _participants.ContainsKey(clientId))
                {
                    _participants[clientId].AddPoint();
                }

                var answerIndex = Round.GetParticipantAnswer(clientId);
                var answer = question.Options.ContainsKey(answerIndex) ? question.Options[answerIndex] : "No answer";
                var message = TriviaQuestionResultServerMessage.Create(Round.RoundId, Round.NumberOfQuestions, question.QuestionId, answer, results[clientId], question.Options[question.CorrectOption]);
                var action = new SendClientMessageServerAction(clientId, message);
                OnNewServerAction?.Invoke(this, new NewServerActionEventArgs(action));
            }

            OnServerLog?.Invoke(this, new ServerLogEventArgs("Game", $"Answers evaluated"));

            MoveToNextState();
            var nextStateAction = new MoveGameToNextStateServerAction();
            OnNewServerAction?.Invoke(this, new NewServerActionEventArgs(nextStateAction));
        }

        /// <summary>
        /// Notifies the participants of a message.
        /// </summary>
        /// <param name="message">The message to send.</param>
        private void NotifyParticipants(TcpServerMessage message, CancellationToken cancellationToken = default)
        {
            foreach (var clientId in Round.ParticipantClientIds)
            {
                if (cancellationToken.IsCancellationRequested) { return; }

                var action = new SendClientMessageServerAction(clientId, message);
                OnNewServerAction?.Invoke(this, new NewServerActionEventArgs(action));
            }
        }

        /// <summary>
        /// Notifies the participants of the end of the round.
        /// </summary>
        private void HandleRoundEnd(CancellationToken cancellationToken = default)
        {
            // Get the overall scores and the ranking of all participants so far
            var overallScores = _participants.Values.OrderByDescending(p => p.Score);
            var overallLeader = overallScores.FirstOrDefault();
            var overallLeaderName = overallLeader?.Name ?? string.Empty;
            var overallLeaderScore = overallLeader?.Score ?? 0;
            var overallRankings = overallScores.Select((Participant, i) => (Participant, i + 1)).ToDictionary(kv => kv.Participant.ClientId, kv => kv.Item2);

            // Get the scores and the ranking of all participants for the current round
            var roundScores = Round.ParticipantClientIds.Select(ClientId => (ClientId, Round.Scoreboard[ClientId]))
                .OrderByDescending(kv => kv.Item2);
            var roundLeader = roundScores.FirstOrDefault();
            _participants.TryGetValue(roundLeader.ClientId, out var roundLeaderParticipant);
            var roundLeaderName = roundLeaderParticipant?.Name ?? "Unknown";
            var roundLeaderScore = roundLeader.Item2;
            var rankings = roundScores.Select((kv, i) => (kv.ClientId, i + 1)).ToDictionary(kv => kv.ClientId, kv => kv.Item2);

            foreach(var clientId in Round.ParticipantClientIds)
            {
                if (cancellationToken.IsCancellationRequested) { return; }

                var overallRank = overallRankings[clientId];
                var rank = rankings[clientId];
                _participants.TryGetValue(clientId, out var participant);
                var overallScore = participant?.Score ?? 0;
                var score = Round.Scoreboard[clientId];

                // Determine if the participant is the overall leader or the round leader
                var oName = clientId == overallLeader?.ClientId ? "You" : overallLeaderName;
                var rName = clientId == roundLeader.ClientId ? "You" : roundLeaderName;

                var message = TriviaRoundEndServerMessage.Create(Round.RoundId, oName, overallLeaderScore, rName, roundLeaderScore, overallScore, score, overallRank, rank);
                var action = new SendClientMessageServerAction(clientId, message);
                OnNewServerAction?.Invoke(this, new NewServerActionEventArgs(action));
            }

            MoveToNextState();
            var nextStateAction = new MoveGameToNextStateServerAction();
            OnNewServerAction?.Invoke(this, new NewServerActionEventArgs(nextStateAction));

            Round.Stop();
            OnServerLog?.Invoke(this, new ServerLogEventArgs("Game", $"Round ended"));
        }

        public override void Dispose()
        {
            _cts.Dispose();
            _gameTask.Dispose();
        }
    }
}