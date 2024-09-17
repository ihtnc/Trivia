using Trivia.Server.TriviaApiClient;

namespace Trivia.Server.TriviaGame
{
    internal class TriviaProvider(IApiClient apiClient) : ITriviaProvider
    {
        private readonly IApiClient _apiClient = apiClient;

        public async Task<IReadOnlyCollection<TriviaRoundQuestion>> GetTriviaQuestionsAsync(TriviaDifficulty? difficulty = null, string? category = null, int numberOfQuestions = 10, CancellationToken cancellationToken = default)
        {
            var difficultyValue = difficulty == TriviaDifficulty.Any ? null : $"{difficulty}".ToLower();
            var categoryId = !string.IsNullOrWhiteSpace(category) && int.TryParse(category, out var parsedId) ? parsedId : null as int?;
            var questions = await _apiClient.GetTriviaQuestionsAsync(difficultyValue, categoryId, numberOfQuestions, cancellationToken);
            return questions.ToTriviaRoundQuestions();
        }

        public async Task<IReadOnlyCollection<TriviaCategory>> GetCategoriesAsync(CancellationToken cancellationToken = default)
        {
            var response = await _apiClient.GetCategoriesAsync(cancellationToken);
            return response.ToTriviaCategories();
        }
    }
}