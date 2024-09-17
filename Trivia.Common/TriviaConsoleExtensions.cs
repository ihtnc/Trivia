namespace Trivia.Common
{
    public static class TriviaConsoleExtensions
    {
        public static ITriviaConsoleWriteStandaloneOperation BeginWrite(this IConsole console, params object[] values)
        {
            var operation = new TriviaConsoleWriteOperation(console, false);
            operation.AddStrings(values);
            return operation;
        }

        public static ITriviaConsoleWriteStandaloneOperation BeginWriteLine(this IConsole console, params object[] values)
        {
            var operation = new TriviaConsoleWriteOperation(console);
            operation.AddStrings(values);
            return operation;
        }

        public static ITriviaConsoleComposer Compose(this IConsole console)
        {
            return new TriviaConsoleComposer(console);
        }
    }
}