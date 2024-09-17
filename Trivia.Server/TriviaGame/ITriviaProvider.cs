namespace Trivia.Server.TriviaGame
{
    internal interface ITriviaProvider
    {
        /// <summary>
        /// Gets trivia questions from the source.
        /// </summary>
        /// <param name="difficulty">The difficulty of the questions to get.</param>
        /// <param name="category">The category of the questions to get.</param>
        /// <param name="numberOfQuestions">The number of questions to get.</param>
        /// <returns>
        /// A collection of trivia questions.
        /// </returns>
        Task<IReadOnlyCollection<TriviaRoundQuestion>> GetTriviaQuestionsAsync(TriviaDifficulty? difficulty = null, string? category = null, int numberOfQuestions = 10, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the list of categories from the source.
        /// </summary>
        /// <returns>
        /// A collection of trivia categories.
        /// </returns>
        Task<IReadOnlyCollection<TriviaCategory>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    }
}