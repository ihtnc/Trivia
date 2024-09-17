namespace Trivia.Server.TriviaGame
{
    internal class TriviaRoundQuestion
    {
        public int QuestionId { get; set; }
        public string Difficulty { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Question { get; set; } = string.Empty;
        public int CorrectOption { get; set; }
        public IReadOnlyDictionary<int, string> Options { get; internal set; } = new Dictionary<int, string>();
    }
}