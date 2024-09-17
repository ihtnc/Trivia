namespace Trivia.Common
{
    public static class TriviaConsoleWriteOperationExtensions
    {
        public static T WithDefaultColor<T>(this T operation, ConsoleColor color) where T : ITriviaConsoleWriteOperation
        {
            operation.DefaultColor = color;
            return operation;
        }

        public static T WithColors<T>(this T operation, params (int Index, ConsoleColor Color)[] values) where T : ITriviaConsoleWriteOperation
        {
            operation.AddColors(values);
            return operation;
        }

        public static T WithRightPadding<T>(this T operation, params (int Index, int TotalWidth)[] values) where T : ITriviaConsoleWriteOperation
        {
            operation.AddRightPadding(values);
            return operation;
        }

        public static T WithLeftPadding<T>(this T operation, params (int Index, int TotalWidth)[] values) where T : ITriviaConsoleWriteOperation
        {
            operation.AddLeftPadding(values);
            return operation;
        }
    }
}