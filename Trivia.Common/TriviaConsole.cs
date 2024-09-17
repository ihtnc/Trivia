namespace Trivia.Common
{
    public interface IConsole
    {
        void WriteLine();
        void WriteLine<T>(T message, ConsoleColor? color = null);
        void Write<T>(T message, ConsoleColor? color = null);
        string ReadLine(ConsoleColor? color = null);
        ConsoleKeyInfo ReadKey(bool intercept = false, ConsoleColor? color = null);
        void Clear();
        void SetCursorPosition(int left, int top);
        (int Left, int Top) GetCursorPosition();
        int CursorLeft { get; set; }
        int CursorTop { get; set; }
        bool KeyAvailable { get; }
        ConsoleColor ForegroundColor { get; }
        ConsoleColor BackgroundColor { get; }
    }

    public class TriviaConsole : IConsole
    {
        public int CursorLeft
        {
            get => Console.CursorLeft;
            set => Console.CursorLeft = value;
        }

        public int CursorTop
        {
            get => Console.CursorTop;
            set => Console.CursorTop = value;
        }

        public bool KeyAvailable => Console.KeyAvailable;

        public ConsoleColor ForegroundColor => Console.ForegroundColor;

        public ConsoleColor BackgroundColor => Console.BackgroundColor;

        public void WriteLine() => Console.WriteLine();

        public void WriteLine<T>(T message, ConsoleColor? color = null)
        {
            if (color.HasValue) { Console.ForegroundColor = color.Value; }
            Console.WriteLine(message);
            Console.ResetColor();
        }

        public void Write<T>(T message, ConsoleColor? color = null)
        {
            if (color.HasValue) { Console.ForegroundColor = color.Value; }
            Console.Write(message);
            Console.ResetColor();
        }

        public string ReadLine(ConsoleColor? color = null)
        {
            if (color.HasValue) { Console.ForegroundColor = color.Value; }
            var value = Console.ReadLine() ?? string.Empty;
            Console.ResetColor();
            return value;
        }

        public ConsoleKeyInfo ReadKey(bool intercept = false, ConsoleColor? color = null)
        {
            if (color.HasValue) { Console.ForegroundColor = color.Value; }
            var key = Console.ReadKey(intercept);
            Console.ResetColor();
            return key;
        }

        public void Clear() => Console.Clear();

        public void SetCursorPosition(int left, int top) => Console.SetCursorPosition(left, top);

        public (int Left, int Top) GetCursorPosition() => (Console.CursorLeft, Console.CursorTop);
    }
}