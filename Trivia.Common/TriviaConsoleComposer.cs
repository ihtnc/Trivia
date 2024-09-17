namespace Trivia.Common
{
    public interface ITriviaConsoleComposer
    {
        IConsole Console { get; }
        bool ResetLinesOnWrite { get; set; }
        bool ResetIndentationOnWrite { get; set; }
        ConsoleColor? DefaultColor { get; set; }
        int IndentSize { get; set; }

        int CurrentIndentLevel { get; }
        IReadOnlyList<TriviaConsoleWriteOperation> WriteOperations { get; }

        ITriviaConsoleWriteStandaloneOperation BeginWrite(string value, ConsoleColor? color = null);
        ITriviaConsoleWriteStandaloneOperation BeginWrite(params object[] values);
        ITriviaConsoleWriteStandaloneOperation BeginWriteLine(string value, ConsoleColor? color = null);
        ITriviaConsoleWriteStandaloneOperation BeginWriteLine(params object[] values);
        ITriviaConsoleWriteOperation AddLine(string value, ConsoleColor? color = null);
        ITriviaConsoleWriteOperation AddLine(params object[] values);
        ITriviaConsoleWriteOperation InsertLine(int lineIndex, string value, ConsoleColor? color = null);
        ITriviaConsoleWriteOperation InsertLine(int lineIndex, params object[] values);
        void AlignLeft(params int[] indices);
        void AlignRight(params int[] indices);
        void Write(bool clearLines = false, bool resetIndentation = false);
        void Reset();
        int Indent();
        int Unindent();
    }

    public class TriviaConsoleComposer(IConsole console) : ITriviaConsoleComposer
    {
        public IConsole Console => console;

        public bool ResetLinesOnWrite { get; set; } = true;
        public bool ResetIndentationOnWrite { get; set; } = true;
        public ConsoleColor? DefaultColor { get; set; } = null;

        private int _indentSize = 2;
        public int IndentSize
        {
            get => _indentSize;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                _indentSize = value;
            }
        }

        public int CurrentIndentLevel { get; private set; } = 0;

        private readonly List<TriviaConsoleWriteOperation> _operations = [];
        public IReadOnlyList<TriviaConsoleWriteOperation> WriteOperations => _operations;

        public ITriviaConsoleWriteStandaloneOperation BeginWrite(string value, ConsoleColor? color = null)
        {
            var operation = new TriviaConsoleWriteOperation(Console, false);
            operation.AddString(AddIndentation([value])[0], color);
            return operation;
        }

        public ITriviaConsoleWriteStandaloneOperation BeginWrite(params object[] values)
        {
            var operation = new TriviaConsoleWriteOperation(Console, false);
            operation.AddStrings(AddIndentation(values));
            return operation;
        }

        public ITriviaConsoleWriteStandaloneOperation BeginWriteLine(string value, ConsoleColor? color = null)
        {
            var operation = new TriviaConsoleWriteOperation(Console, true);
            operation.AddString(AddIndentation([value])[0], color);
            return operation;
        }

        public ITriviaConsoleWriteStandaloneOperation BeginWriteLine(params object[] values)
        {
            var operation = new TriviaConsoleWriteOperation(Console, true);
            operation.AddStrings(AddIndentation(values));
            return operation;
        }

        public ITriviaConsoleWriteOperation AddLine(string value, ConsoleColor? color = null)
        {
            var operation = new TriviaConsoleWriteOperation(Console, true);
            operation.AddString(AddIndentation([value])[0], color);
            _operations.Add(operation);
            return operation;
        }

        public ITriviaConsoleWriteOperation AddLine(params object[] values)
        {
            var operation = new TriviaConsoleWriteOperation(Console, true);
            operation.AddStrings(AddIndentation(values));
            _operations.Add(operation);
            return operation;
        }

        public ITriviaConsoleWriteOperation InsertLine(int lineIndex, string value, ConsoleColor? color = null)
        {
            if (lineIndex < 0 || lineIndex >= _operations.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(lineIndex));
            }

            var operation = new TriviaConsoleWriteOperation(Console, true);
            operation.AddString(AddIndentation([value])[0], color);
            _operations.Insert(lineIndex, operation);
            return operation;
        }

        public ITriviaConsoleWriteOperation InsertLine(int lineIndex, params object[] values)
        {
            if (lineIndex < 0 || lineIndex >= _operations.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(lineIndex));
            }

            var operation = new TriviaConsoleWriteOperation(Console, true);
            operation.AddStrings(AddIndentation(values));
            _operations.Insert(lineIndex, operation);
            return operation;
        }

        public void AlignLeft(params int[] indices)
        {
            foreach (var index in indices)
            {
                if (index < 0) { throw new ArgumentOutOfRangeException(nameof(indices)); }

                var operations = _operations.Where(o => o.Parts.Count > index);

                var longest = operations
                    .OrderByDescending(o => o.Parts[index].Length)
                    .Select(o => o.Parts[index].Length)
                    .Take(1)
                    .SingleOrDefault();

                foreach (var operation in operations)
                {
                    if (index >= operation.Parts.Count) { continue; }
                    operation.AddRightPadding((index, longest));
                }
            }
        }

        public void AlignRight(params int[] indices)
        {
            foreach (var index in indices)
            {
                if (index < 0) { throw new ArgumentOutOfRangeException(nameof(indices)); }

                var operations = _operations.Where(o => o.Parts.Count > index);

                var longest = operations
                    .OrderByDescending(o => o.Parts[index].Length)
                    .Select(o => o.Parts[index].Length)
                    .Take(1)
                    .SingleOrDefault();

                foreach (var operation in operations)
                {
                    operation.AddLeftPadding((index, longest));
                }
            }
        }

        public void Write(bool resetLines = false, bool resetIndentation = false)
        {
            foreach (var operation in _operations)
            {
                operation.DefaultColor ??= DefaultColor;
                operation.Run();
            }

            if (resetLines || ResetLinesOnWrite)
            {
                _operations.Clear();
            }

            if (resetIndentation || ResetIndentationOnWrite)
            {
                CurrentIndentLevel = 0;
            }
        }

        public void Reset()
        {
            _operations.Clear();
            CurrentIndentLevel = 0;
        }

        public int Indent()
        {
            CurrentIndentLevel++;
            return CurrentIndentLevel;
        }

        public int Unindent()
        {
            if (CurrentIndentLevel > 0)
            {
                CurrentIndentLevel--;
            }

            return CurrentIndentLevel;
        }

        private object[] AddIndentation(object[] values)
        {
            if (CurrentIndentLevel == 0 || values.Length == 0)
            {
                return values;
            }

            var indentation = new string(' ', IndentSize * CurrentIndentLevel);
            return values.Select((v, i) => i != 0 ? v : $"{indentation}{v}").ToArray();
        }
    }
}