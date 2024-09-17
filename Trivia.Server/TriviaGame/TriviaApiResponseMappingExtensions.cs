using System.Web;
using Trivia.Server.TriviaApiClient;

namespace Trivia.Server.TriviaGame
{
    internal static class TriviaApiResponseMappingExtensions
    {
        /// <summary>
        /// Converts a collection of trivia api questions to a collection of trivia round questions.
        /// </summary>
        /// <param name="questions">The collection of trivia api questions to convert.</param>
        /// <returns>
        /// A collection of trivia round questions. The order of the questions are also randomised as part of this conversion.
        /// </returns>
        public static IReadOnlyCollection<TriviaRoundQuestion> ToTriviaRoundQuestions(this IEnumerable<TriviaApiQuestion> questions)
        {
            var random = new Random();
            var randomisedQuestions = questions.Select(ToTriviaRoundQuestion).OrderBy(x => random.Next()).ToArray();
            var numberedList = randomisedQuestions.Select((question, index) =>
            {
                question.QuestionId = index + 1;
                return question;
            });

            return [.. numberedList];
        }

        /// <summary>
        /// Converts a trivia api question to a trivia round question.
        /// </summary>
        /// <param name="question">The trivia api question to convert.</param>
        /// <returns>
        /// A trivia round question. The order of options are also randomised as part of this conversion.
        /// </returns>
        public static TriviaRoundQuestion ToTriviaRoundQuestion(this TriviaApiQuestion question)
        {
            var totalOptions = question.OtherOptions.Count + 1;
            var correctOptionIndex = 0;
            Dictionary<int, string> options = [];

            if (question.Type.Equals("multiple", StringComparison.OrdinalIgnoreCase))
            {
                // randomise the order of the options
                var random = new Random();
                correctOptionIndex = random.Next(0, totalOptions);
                var randomOptions = question.OtherOptions.OrderBy(x => random.Next()).ToArray();
                var optionsIndex = 0;

                for (var i = 0; i < totalOptions; i++)
                {
                    var option = i == correctOptionIndex ? question.CorrectOption : randomOptions[optionsIndex];
                    if (i != correctOptionIndex) { optionsIndex++; }

                    options.Add(i + 1, HttpUtility.HtmlDecode(option));
                }
            }
            else
            {
                options.Add(1, "True");
                options.Add(2, "False");
                correctOptionIndex = question.CorrectOption.Equals("True", StringComparison.OrdinalIgnoreCase) ? 0 : 1;
            }

            return new TriviaRoundQuestion
            {
                Category = HttpUtility.HtmlDecode(question.Category),
                Difficulty = question.Difficulty,
                Question = HttpUtility.HtmlDecode(question.Question),
                CorrectOption = correctOptionIndex + 1,
                Options = options
            };
        }

        /// <summary>
        /// Converts a collection of trivia api categories to a collection of trivia categories.
        /// </summary>
        /// <param name="categories">The collection of trivia api categories to convert.</param>
        /// <returns>
        /// A collection of trivia categories.
        /// </returns>
        public static IReadOnlyCollection<TriviaCategory> ToTriviaCategories(this IEnumerable<TriviaApiCategory> categories)
        {
            return categories.Select(ToTriviaCategory).ToArray();
        }

        /// <summary>
        /// Converts a trivia api category to a trivia category.
        /// </summary>
        /// <param name="category">The trivia api category to convert.</param>
        /// <returns>
        /// A trivia category.
        /// </returns>
        public static TriviaCategory ToTriviaCategory(this TriviaApiCategory category)
        {
            return new TriviaCategory
            {
                Id = category.Id,
                Category = HttpUtility.HtmlDecode(category.Name)
            };
        }
    }
}