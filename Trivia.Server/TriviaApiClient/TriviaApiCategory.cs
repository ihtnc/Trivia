using System.Text.Json.Serialization;

namespace Trivia.Server.TriviaApiClient
{
    internal class TriviaApiCategory
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }
}