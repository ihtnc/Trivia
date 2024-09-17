using System.Text.Json.Serialization;

namespace Trivia.Server.TriviaApiClient
{
    internal class TriviaApiQuestionsResponse
    {
        [JsonPropertyName("results")]
        public IReadOnlyCollection<TriviaApiQuestion> Questions { get; set; } = [];
    }

    internal class TriviaApiCategoriesResponse
    {
        [JsonPropertyName("trivia_categories")]
        public IReadOnlyCollection<TriviaApiCategory> Categories { get; set; } = [];
    }
}