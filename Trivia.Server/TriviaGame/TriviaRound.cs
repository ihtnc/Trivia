namespace Trivia.Server.TriviaGame
{
    public enum TriviaDifficulty
    {
        Any,
        Easy,
        Medium,
        Hard
    }

    internal interface ITriviaRound
    {
        /// <summary>
        /// Gets the ID of the round.
        /// </summary>
        int RoundId { get; }

        /// <summary>
        /// Gets the difficulty of the questions in the round.
        /// </summary>
        TriviaDifficulty Difficulty { get; }

        /// <summary>
        /// Gets the category of the questions in the round.
        /// </summary>
        TriviaCategory? Category { get; }

        /// <summary>
        /// Gets the remaining questions left in the round.
        /// </summary>
        IReadOnlyCollection<TriviaRoundQuestion> RemainingQuestions { get; }

        /// <summary>
        /// Gets the number of questions in the round.
        /// </summary>
        int NumberOfQuestions { get; }

        /// <summary>
        /// Gets the client IDs of the participants in the round.
        /// </summary>
        IReadOnlyCollection<int> ParticipantClientIds { get; }

        /// <summary>
        /// Gets the scoreboard of the participants in the round.
        /// </summary>
        IReadOnlyDictionary<int, int> Scoreboard { get; }

        /// <summary>
        /// Gets the current question in the round.
        /// </summary>
        /// <returns>
        /// The current question in the round, or <see langword="null"/> if the round has not started or is over.
        /// </returns>
        TriviaRoundQuestion? CurrentQuestion { get; }

        /// <summary>
        /// Gets the current answers to the current question.
        /// </summary>
        IReadOnlyCollection<TriviaRoundAnswer> CurrentAnswers { get; }

        /// <summary>
        /// Gets the current results of the participants' answers to the question.
        /// </summary>
        /// <value></value>
        IReadOnlyDictionary<int, bool> CurrentResults { get; }

        /// <summary>
        /// Gets a value indicating whether the round has started.
        /// </summary>
        bool IsRoundStarted { get; }

        /// <summary>
        /// Gets a value indicating whether the round is over.
        /// </summary>
        bool IsRoundOver { get; }

        /// <summary>
        /// Starts a new trivia round.
        /// </summary>
        /// <param name="roundId">The ID of the round to start.</param>
        /// <param name="participants">The participants to enlist in the round.</param>
        /// <param name="difficulty">
        /// The difficulty of the questions to fetch. If <see langword="null"/>, the API will return questions of any difficulty.
        /// </param>
        /// <param name="category">
        /// The category of the questions to fetch. If <see langword="null"/>, the API will return questions of any category.
        /// /// </param>
        /// <param name="numberOfQuestions">
        /// The number of questions to fetch from the API. The default is 10.
        /// </param>
        Task Start(int roundId, IReadOnlyCollection<int> participantClientIds, TriviaDifficulty? difficulty = TriviaDifficulty.Any, TriviaCategory? category = null, int numberOfQuestions = 10, CancellationToken cancellationToken = default);

        /// <summary>
        /// Stops the current round.
        /// </summary>
        void Stop();

        /// <summary>
        /// Adds a participant's answer to the current question.
        /// </summary>
        /// <param name="answer">The answer to add.</param>
        /// <returns>
        /// <see langword="true"/> if the answer was successfully added; otherwise, <see langword="false"/>.
        /// </returns>
        bool AddParticipantAnswer(TriviaRoundAnswer answer);

        /// <summary>
        /// Gets the answer of a participant to the current question.
        /// </summary>
        /// <param name="clientId">The client ID of the participant to get the answer for.
        /// </param>
        /// <returns>
        /// The answer of the participant to the current question if provided; otherwise, 0.
        /// </returns>
        int GetParticipantAnswer(int clientId);

        /// <summary>
        /// Evaluates the answers of the participants to the current question and awards points to the correct answers.
        /// </summary>
        /// <returns>
        /// A dictionary containing the client IDs of the participants and whether their answers were correct.
        /// </returns>
        void EvaluateAnswers();

        /// <summary>
        /// Moves to the next question in the round.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if the round has more questions; otherwise, <see langword="false"/>.
        /// </returns>
        bool NextQuestion();

        /// <summary>
        /// Resets the round to its initial state.
        /// </summary>
        void Reset();
    }

    internal class TriviaRound(ITriviaProvider provider) : ITriviaRound
    {
        private bool _started;

        public int RoundId { get; private set; }

        public TriviaDifficulty Difficulty { get; private set; } = TriviaDifficulty.Any;

        public TriviaCategory? Category { get; private set; } = null;

        public IReadOnlyCollection<TriviaRoundQuestion> RemainingQuestions => _questions;
        private Queue<TriviaRoundQuestion> _questions = [];

        public int NumberOfQuestions { get; private set; }

        public IReadOnlyCollection<int> ParticipantClientIds => _scoreboard.Keys;

        public IReadOnlyDictionary<int, int> Scoreboard => _scoreboard;
        private Dictionary<int, int> _scoreboard = [];

        public TriviaRoundQuestion? CurrentQuestion => IsRoundStarted && _questions.Count > 0 ? _questions.Peek() : null;

        public IReadOnlyCollection<TriviaRoundAnswer> CurrentAnswers => _answers.Values;
        private Dictionary<int, TriviaRoundAnswer> _answers = [];

        public IReadOnlyDictionary<int, bool> CurrentResults => _results;
        private Dictionary<int, bool> _results = [];

        public bool IsRoundStarted => _started;

        public bool IsRoundOver => _questions.Count == 0;

        private readonly ITriviaProvider _provider = provider;

        /// <summary>
        /// Loads the questions into the round.
        /// </summary>
        private void LoadQuestions(IReadOnlyCollection<TriviaRoundQuestion> questions)
        {
            _questions = new Queue<TriviaRoundQuestion>(questions);
            NumberOfQuestions = questions.Count;
        }

        /// <summary>
        /// Starts a new trivia round.
        /// </summary>
        /// <param name="roundId">The ID of the round to start.</param>
        /// <param name="participants">The participants to enlist in the round.</param>
        /// <param name="difficulty">
        /// The difficulty of the questions to fetch. If <see langword="null"/>, the API will return questions of any difficulty.
        /// </param>
        /// <param name="category">
        /// The category of the questions to fetch. If <see langword="null"/>, the API will return questions of any category.
        /// </param>
        /// <param name="numberOfQuestions">
        /// The number of questions to fetch from the API. The default is 10.
        /// </param>
        public async Task Start(int roundId, IReadOnlyCollection<int> participantClientIds, TriviaDifficulty? difficulty = TriviaDifficulty.Any, TriviaCategory? category = null, int numberOfQuestions = 10, CancellationToken cancellationToken = default)
        {
            Reset();

            _scoreboard = participantClientIds.ToDictionary(k => k, v => 0);
            _results = participantClientIds.ToDictionary(k => k, v => false);

            var questions = await _provider.GetTriviaQuestionsAsync(difficulty, $"{category?.Id}", numberOfQuestions, cancellationToken);
            LoadQuestions(questions);

            RoundId = roundId;
            Difficulty = difficulty ?? TriviaDifficulty.Any;
            Category = category;
            _started = true;
        }

        /// <summary>
        /// Stops the current round.
        /// </summary>
        public void Stop()
        {
            _started = false;
        }

        /// <summary>
        /// Adds a participant's answer to the current question.
        /// </summary>
        /// <param name="answer">The answer to add.</param>
        /// <returns>
        /// <see langword="true"/> if the answer was successfully added; otherwise, <see langword="false"/>.
        /// </returns>
        public bool AddParticipantAnswer(TriviaRoundAnswer answer)
        {
            if (answer.RoundId != RoundId || !IsRoundStarted || IsRoundOver || answer.QuestionId != CurrentQuestion?.QuestionId)
            {
                return false;
            }

            if (!_scoreboard.ContainsKey(answer.ClientId)) { return false; }

            _answers.Add(answer.ClientId, answer);
            return true;
        }

        /// <summary>
        /// Gets the answer of a participant to the current question.
        /// </summary>
        /// <param name="clientId">The client ID of the participant to get the answer for.
        /// </param>
        /// <returns>
        /// The answer of the participant to the current question if provided; otherwise, 0.
        /// </returns>
        public int GetParticipantAnswer(int clientId)
        {
            if (!_answers.TryGetValue(clientId, out TriviaRoundAnswer? value)) { return 0; }

            return value.Answer;
        }

        /// <summary>
        /// Evaluates the answers of the participants to the current question.
        /// </summary>
        public void EvaluateAnswers()
        {
            if (!IsRoundStarted || IsRoundOver) { return; }

            var correctAnswers = CurrentAnswers.Where(a => a.Answer == CurrentQuestion?.CorrectOption);

            foreach (var answer in correctAnswers)
            {
                if (_results.ContainsKey(answer.ClientId))
                {
                    _results[answer.ClientId] = true;
                }
            }
        }

        /// <summary>
        /// Award points to the participants with correct answers and moves to the next question in the round.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if the round has more questions; otherwise, <see langword="false"/>.
        /// </returns>
        public bool NextQuestion()
        {
            if (!IsRoundStarted || IsRoundOver) { return false; }

            _questions.Dequeue();
            _answers.Clear();

            foreach(var result in _results)
            {
                if (result.Value && _scoreboard.TryGetValue(result.Key, out int score))
                {
                    _scoreboard[result.Key] = ++score;
                }
            }

            _results = _results.ToDictionary(k => k.Key, v => false);

            return !IsRoundOver;
        }

        /// <summary>
        /// Resets the round to its initial state.
        /// </summary>
        public void Reset()
        {
            RoundId = 0;
            _questions.Clear();
            _answers.Clear();
            _scoreboard.Clear();
            _started = false;
        }
    }
}