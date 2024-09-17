using Flurl;
using Flurl.Http;

namespace Trivia.Server.TriviaApiClient
{
    /// <summary>
    /// API client for the Open Trivia Database.
    /// </summary>
    internal interface IApiClient
    {
        /// <summary>
        /// Gets trivia questions from the Open Trivia Database.
        /// </summary>
        /// <param name="difficulty">The difficulty of the questions to get.</param>
        /// <param name="categoryId">The category of the questions to get.</param>
        /// <param name="numberOfQuestions">The number of questions to get.</param>
        /// <returns>
        /// A collection of trivia questions.
        /// </returns>
        Task<IReadOnlyCollection<TriviaApiQuestion>> GetTriviaQuestionsAsync(string? difficulty = null, int? categoryId = null, int numberOfQuestions = 10, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the list of categories from the Open Trivia Database.
        /// </summary>
        /// <returns>
        /// A collection of trivia categories.
        /// </returns>
        Task<IReadOnlyCollection<TriviaApiCategory>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    }

    internal class ApiClient : IApiClient
    {
        public async Task<IReadOnlyCollection<TriviaApiQuestion>> GetTriviaQuestionsAsync(string? difficulty = null, int? categoryId = null, int numberOfQuestions = 10, CancellationToken cancellationToken = default)
        {
            var url = "https://opentdb.com/api.php"
                .SetQueryParam("amount", numberOfQuestions);

            if (categoryId.HasValue)
            {
                url = url.SetQueryParam("category", categoryId);
            }

            if (!string.IsNullOrWhiteSpace(difficulty))
            {
                url = url.SetQueryParam("difficulty", difficulty);
            }

            var response = await url.GetJsonAsync<TriviaApiQuestionsResponse>(cancellationToken: cancellationToken);

            return response.Questions;
        }

        public async Task<IReadOnlyCollection<TriviaApiCategory>> GetCategoriesAsync(CancellationToken cancellationToken = default)
        {
            var url = "https://opentdb.com/api_category.php";

            var response = await url.GetJsonAsync<TriviaApiCategoriesResponse>(cancellationToken: cancellationToken);
            return response.Categories;
        }
    }
}


