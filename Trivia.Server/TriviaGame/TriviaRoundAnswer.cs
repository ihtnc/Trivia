namespace Trivia.Server.TriviaGame
{
    internal class TriviaRoundAnswer
    {
        public int RoundId { get; set; }
        public int QuestionId { get; set; }
        public int ClientId { get; set; }
        public int Answer { get; set; }
        public bool? IsCorrect { get; set; }
    }
}