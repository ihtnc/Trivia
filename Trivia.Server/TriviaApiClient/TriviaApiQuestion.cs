using System.Text.Json.Serialization;

namespace Trivia.Server.TriviaApiClient
{
    internal class TriviaApiQuestion
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("difficulty")]
        public string Difficulty { get; set; } = string.Empty;

        [JsonPropertyName("category")]
        public string Category { get; set; } = string.Empty;

        [JsonPropertyName("question")]
        public string Question { get; set; } = string.Empty;

        [JsonPropertyName("correct_answer")]
        public string CorrectOption { get; set; } = string.Empty;

        [JsonPropertyName("incorrect_answers")]
        public IReadOnlyCollection<string> OtherOptions { get; set; } = [];
    }
}