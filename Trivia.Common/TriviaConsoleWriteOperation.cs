namespace Trivia.Common
{
    public interface ITriviaConsoleWriteOperation
    {
        bool WriteLine { get; }

        public IReadOnlyList<string> Parts { get; }

        ConsoleColor? DefaultColor { get; set; }

        void AddString<T>(T value, ConsoleColor? color = null);
        void AddStrings(params object[] values);
        void AddColors(params (int Index, ConsoleColor Color)[] colors);
        void AddRightPadding(params (int Index, int TotalWidth)[] paddings);
        void AddLeftPadding(params (int Index, int TotalWidth)[] paddings);
        public string RawString { get; }
    }

    public interface ITriviaConsoleWriteStandaloneOperation : ITriviaConsoleWriteOperation
    {
        void Run();
    }

    public class TriviaConsoleWriteOperation(IConsole console, bool writeLine = true) : ITriviaConsoleWriteStandaloneOperation
    {
        private readonly IConsole _console = console;

        public bool WriteLine { get; } = writeLine;

        private List<string> _parts = [];
        public IReadOnlyList<string> Parts => _parts;

        public ConsoleColor? DefaultColor { get; set; } = null;

        private Dictionary<int, ConsoleColor> _colors = [];
        private Dictionary<int, int> _padding = [];

        public void AddString<T>(T value, ConsoleColor? color = null)
        {
            _parts.Add($"{value}");

            if (!color.HasValue) { return; }

            var index = _parts.Count - 1;
            if (_colors.ContainsKey(index))
            {
                _colors[index] = color.Value;
            }
            else
            {
                _colors.Add(index, color.Value);
            }
        }

        public void AddStrings(params object[] values)
        {
            foreach (var value in values)
            {
                AddString(value);
            }
        }

        public void AddColors(params (int Index, ConsoleColor Color)[] colors)
        {
            foreach (var (index, color) in colors)
            {
                if (_colors.ContainsKey(index))
                {
                    _colors[index] = color;
                }
                else
                {
                    _colors.Add(index, color);
                }
            }
        }

        public void AddRightPadding(params (int Index, int TotalWidth)[] paddings)
        {
            foreach (var (Index, TotalWidth) in paddings)
            {
                if (_padding.ContainsKey(Index))
                {
                    _padding[Index] = TotalWidth;
                }
                else
                {
                    _padding.Add(Index, TotalWidth);
                }
            }
        }

        public void AddLeftPadding(params (int Index, int TotalWidth)[] paddings)
        {
            foreach (var (Index, TotalWidth) in paddings)
            {
                if (_padding.ContainsKey(Index))
                {
                    _padding[Index] = -TotalWidth;
                }
                else
                {
                    _padding.Add(Index, -TotalWidth);
                }
            }
        }

        public string RawString => string.Join("", _parts.Select(GetPaddedValue));

        public void Run()
        {
            for (int i = 0; i < _parts.Count; i++)
            {
                var part = GetPaddedValue(_parts[i], i);
                var color = GetColor(i);

                _console.Write(part, color);
            }

            if (WriteLine)
            {
                _console.WriteLine();
            }
        }

        private string GetPaddedValue(string value, int index)
        {
            if (_padding.ContainsKey(index))
            {
                value = _padding[index] > 0 ? value.PadRight(_padding[index]) : value.PadLeft(-_padding[index]);
            }

            return value;
        }

        private ConsoleColor? GetColor(int index)
        {
            return _colors.ContainsKey(index) ? _colors[index] : DefaultColor;
        }
    }
}