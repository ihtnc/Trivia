using System.Net;
using Trivia.Common;

namespace Trivia.Client
{
    public class Program
    {
        private static int _roundId;
        private static int _questionId;
        private static int _selectedAnswerIndex;
        private static int _answerIndex;

        private enum TriviaState
        {
            Initial,
            WaitingToStart,
            RoundStart,
            NewQuestion,
            QuestionAnswered,
            Result
        }

        private static TriviaState _state = TriviaState.Initial;

        private static IConsole _console = new TriviaConsole();

        private static TriviaClient _client;

        static Program()
        {
            _client = new TriviaClient();
            _client.OnRoundStart += OnRoundStart;
            _client.OnNewQuestion += OnNewQuestion;
            _client.OnQuestionResult += OnQuestionResult;
            _client.OnRoundEnd += OnRoundEnd;
            _client.OnRoundDetails += OnRoundDetails;
            _client.OnError += OnError;
        }

        public static async Task Main(string[] args)
        {
            try
            {
                _console.Clear();
                _console.WriteLine("Trivia Client");

                while(!_client.IsConnected)
                {
                    (IPEndPoint endpoint, string name) = CaptureConnectionDetails();
                    await _client.ConnectAsync(endpoint, name);
                }

                _console.WriteLine("Connected to server.");

                var input = string.Empty;
                _state = TriviaState.Initial;

                do
                {
                    input = string.Empty;
                    if (_console.KeyAvailable)
                    {
                        input = $"{_console.ReadKey(true).KeyChar}";
                    }

                    switch (_state)
                    {
                        case TriviaState.Initial:
                            _console.WriteLine("Waiting for round to start...");
                            DisplayPressQToQuit();
                            _console.WriteLine();
                            _state = TriviaState.WaitingToStart;
                            break;

                        case TriviaState.NewQuestion:
                            if (string.IsNullOrWhiteSpace(input)) { continue; }

                            if (int.TryParse(input, out var answer))
                            {
                                DisplayAnswerGuide(answer);
                                _selectedAnswerIndex = answer;
                            }

                            if (_selectedAnswerIndex > 0 && input.Equals("C", StringComparison.OrdinalIgnoreCase))
                            {
                                var result = await _client.SendAnswerAsync(_roundId, _questionId, _selectedAnswerIndex);
                                if (result)
                                {
                                    _console.WriteLine(_selectedAnswerIndex, ConsoleColor.Yellow);
                                    ClearAnswerGuide();
                                    _console.WriteLine();
                                    _console.WriteLine("Waiting for result...");
                                    _console.WriteLine();
                                    _answerIndex = _selectedAnswerIndex;
                                    _state = TriviaState.QuestionAnswered;
                                }
                            }

                            break;
                    }

                    if (input.Equals("P", StringComparison.OrdinalIgnoreCase) && _state != TriviaState.NewQuestion)
                    {
                        DisplayConnectivityStatus();
                        _console.WriteLine("Requesting round details...");
                        _console.WriteLine();
                        await _client.SendRequestRoundDetailsAsync();
                    }

                } while (!input.Equals("Q", StringComparison.OrdinalIgnoreCase));

                await _client.Disconnect();
            }
            catch (Exception ex)
            {
                _console.WriteLine(ex.Message, ConsoleColor.Red);
            }
            finally
            {
                if (_state == TriviaState.NewQuestion)
                {
                    _console.WriteLine();
                    ClearAnswerGuide();
                    _console.WriteLine();
                }

                _console.WriteLine("Quitting...");
                _console.WriteLine("Press any key to exit...");
                _console.ReadKey(true);
            }
        }

        private static (IPEndPoint Endpoint, string Name ) CaptureConnectionDetails()
        {
            _console.Write("Server: ");
            var server = _console.ReadLine(ConsoleColor.Yellow) ?? string.Empty;

            _console.Write("Port: ");
            var port = int.Parse(_console.ReadLine(ConsoleColor.Yellow) ?? string.Empty);

            var serverEndPoint = new IPEndPoint(IPAddress.Parse(server), port);

            _console.Write("Name: ");
            var name = _console.ReadLine(ConsoleColor.Yellow) ?? string.Empty;

            return (serverEndPoint, name);
        }

        private static void OnRoundStart(object? sender, RoundStartEventArgs e)
        {
            var composer = _console.Compose();
            composer.AddLine("Round ", e.RoundId, " starting...")
                .WithColors((1, ConsoleColor.Yellow));
            composer.AddLine("Questions: ", e.QuestionCount)
                .WithColors((1, ConsoleColor.Yellow));
            composer.AddLine("Participants: ", e.ParticipantCount)
                .WithColors((1, ConsoleColor.Yellow));
            composer.AddLine();
            composer.Write();

            _roundId = e.RoundId;
            _state = TriviaState.RoundStart;
        }

        private static void OnNewQuestion(object? sender, NewQuestionEventArgs e)
        {
            var composer = _console.Compose();
            var headerLine = composer.AddLine("Trivia Question");
            var headerLineLength = headerLine.RawString.Length;
            composer.AddLine(new string('=', headerLineLength));

            composer.Indent();
            composer.AddLine("Round: ", e.RoundId)
                .WithColors((1, ConsoleColor.Yellow));
            composer.AddLine("Question: ", e.QuestionId, " of ", e.QuestionCount)
                .WithColors((1, ConsoleColor.Yellow), (3, ConsoleColor.Yellow));

            var color = e.Difficulty.ToUpper() switch
            {
                "EASY" => ConsoleColor.Green,
                "MEDIUM" => ConsoleColor.Yellow,
                "HARD" => ConsoleColor.Red,
                _ => ConsoleColor.Yellow
            };
            composer.AddLine("Category: ", e.Category, " (", e.Difficulty, ")")
                .WithColors((1, ConsoleColor.Yellow), (3, color));

            composer.AddLine();
            composer.AddLine(e.Question, ConsoleColor.Yellow);
            composer.AddLine();

            composer.AddLine("Options:");
            composer.Indent();
            foreach (var option in e.Answers)
            {
                composer.AddLine("[", option.Key, "] ", option.Value)
                    .WithColors((1, ConsoleColor.Yellow));
            }
            composer.Unindent();
            composer.AddLine();
            composer.Write();

            DisplayAnswerGuide();

            _answerIndex = 0;
            _selectedAnswerIndex = 0;
            _questionId = e.QuestionId;
            _state = TriviaState.NewQuestion;
        }

        private static void OnQuestionResult(object? sender, QuestionResultEventArgs e)
        {
            if (_answerIndex == 0)
            {
                _console.WriteLine();
                ClearAnswerGuide();
                _console.WriteLine();
            }

            var composer = _console.Compose();
            var headerLine = composer.AddLine("Trivia Result");
            var headerLineLength = headerLine.RawString.Length;
            composer.AddLine(new string('=', headerLineLength));

            composer.Indent();
            composer.AddLine("Round: ", e.RoundId)
                .WithColors((1, ConsoleColor.Yellow));
            composer.AddLine("Question: ", e.QuestionId, " of ", e.QuestionCount)
                .WithColors((1, ConsoleColor.Yellow), (3, ConsoleColor.Yellow));

            var color = e.Correct ? ConsoleColor.Green : ConsoleColor.Red;
            composer.AddLine("Answer: ", e.Answer, " (", e.Correct ? "Correct" : "Incorrect", ")")
                .WithColors((1, ConsoleColor.Yellow), (3, color));

            if (!e.Correct)
            {
                composer.AddLine("Correct answer: ", e.CorrectAnswer)
                    .WithColors((1, ConsoleColor.Yellow));
            }

            composer.Unindent();
            composer.AddLine();
            var waitMessage = e.QuestionCount == e.QuestionId ? "Waiting for round results..." : "Waiting for next question...";
            composer.AddLine(waitMessage);
            composer.AddLine();

            composer.Write();

            _state = TriviaState.Result;
        }

        private static void OnRoundEnd(object? sender, RoundEndEventArgs e)
        {
            if (_answerIndex == 0)
            {
                _console.WriteLine();
                ClearAnswerGuide();
            }

            var composer = _console.Compose();
            var headerLine = composer.AddLine("Round End");
            var headerLineLength = headerLine.RawString.Length;
            composer.AddLine(new string('=', headerLineLength));

            composer.Indent();
            composer.AddLine("Round: ", e.RoundId)
                .WithColors((1, ConsoleColor.Yellow));
            composer.AddLine();

            composer.AddLine("Overall Leaderboard");

            composer.Indent();
            if (e.OverallRank > 1)
            {
                composer.AddLine("Rank 1: ", e.OverallLeader, " (", e.OverallLeaderScore, " point/s)")
                    .WithColors((1, ConsoleColor.Yellow), (3, ConsoleColor.Yellow));
            }

            composer.AddLine("Rank ", e.OverallRank, ": You (", e.OverallScore, " point/s)")
                .WithColors((1, ConsoleColor.Yellow), (3, ConsoleColor.Yellow));
            composer.AddLine();
            composer.Unindent();

            composer.AddLine("Round ", e.RoundId, " Leaderboard")
                .WithColors((1, ConsoleColor.Yellow));

            composer.Indent();
            if (e.Rank > 1)
            {
                composer.AddLine("Rank 1: ", e.RoundLeader, " (", e.RoundLeaderScore, " point/s)")
                    .WithColors((1, ConsoleColor.Yellow), (3, ConsoleColor.Yellow));
            }

            composer.AddLine("Rank ", e.Rank, ": You (", e.Score, " point/s)")
                .WithColors((1, ConsoleColor.Yellow), (3, ConsoleColor.Yellow));
            composer.AddLine();

            composer.Write();

            _state = TriviaState.Initial;
        }

        private static void OnRoundDetails(object? sender, RoundDetailsEventArgs e)
        {
            var composer = _console.Compose();
            var headerLine = composer.AddLine("Round Details");
            var headerLineLength = headerLine.RawString.Length;
            composer.AddLine(new string('=', headerLineLength));

            composer.Indent();
            composer.AddLine("Round: ", e.RoundId, " (", e.Status, ")")
                .WithColors((1, ConsoleColor.Yellow), (3, ConsoleColor.Yellow));

            composer.AddLine("Question: ", e.QuestionId, " of ", e.QuestionCount)
                .WithColors((1, ConsoleColor.Yellow), (3, ConsoleColor.Yellow));

            var color = e.Difficulty.ToUpper() switch
            {
                "EASY" => ConsoleColor.Green,
                "MEDIUM" => ConsoleColor.Yellow,
                "HARD" => ConsoleColor.Red,
                _ => ConsoleColor.Yellow
            };
            composer.AddLine("Category: ", e.Category, " (", e.Difficulty, ")")
                .WithColors((1, ConsoleColor.Yellow), (3, color));

            var isOnlyParticipant = e.ParticipantCount == 1 && e.IsParticipant;
            var participantText1 = isOnlyParticipant ? "You" : $"{e.ParticipantCount}";
            participantText1 = e.ParticipantCount == 0 ? "None" : participantText1;
            var participantText2 = !isOnlyParticipant && e.IsParticipant ? " (including you)" : "";
            composer.AddLine("Participants: ", participantText1, participantText2)
                .WithColors((1, ConsoleColor.Yellow));
            composer.AddLine();

            composer.Write();
        }

        private static void OnError(object? sender, ErrorEventArgs e)
        {
            _console.WriteLine(e.ErrorMessage, ConsoleColor.Red);
        }

        private static void DisplayPressQToQuit()
        {
            _console.BeginWriteLine("Press [", "P", "] to ping or [", "Q", "] to quit.")
                .WithColors((1, ConsoleColor.Yellow), (3, ConsoleColor.Yellow))
                .Run();
        }

        private static void DisplayAnswerGuide(int answerIndex = 0)
        {
            var composer = _console.Compose();
            composer.Console.CursorLeft = 0;
            var currentLine = composer.Console.CursorTop;

            composer.Indent();
            var title = "Answer: ";
            var currentColumn = title.Length + (composer.IndentSize * composer.CurrentIndentLevel);
            var defaultValue = "Choose a number";
            if (answerIndex > 0)
            {
                composer.AddLine(title, $"{answerIndex}")
                    .WithColors((1, ConsoleColor.Yellow))
                    .AddRightPadding((1, defaultValue.Length));
            }
            else
            {
                composer.AddLine(title, defaultValue)
                    .WithColors((1, ConsoleColor.DarkGray));
            }

            composer.AddLine("Press [", "C", "] to confirm.")
                .WithDefaultColor(ConsoleColor.DarkGray)
                .WithColors((1, ConsoleColor.Yellow));
            composer.AddLine();

            composer.Write();
            composer.Console.SetCursorPosition(currentColumn, currentLine);
        }

        private static void DisplayConnectivityStatus()
        {
            var composer = _console.Compose();
            var connectedText1 = _client.IsConnected ? "Connected" : "Not";
            var connectedText2 = _client.IsConnected ? "" : " connected";
            var conjunction = _client.IsConnected == _client.IsReceivingMessages ? " and " : " but ";
            var receivingText1 = _client.IsReceivingMessages ? "receiving" : "not";
            var receivingText2 = _client.IsReceivingMessages ? " messages" : " receiving messages";

            composer.AddLine("Status: ", connectedText1, connectedText2, conjunction, receivingText1, receivingText2)
                .WithColors((1, _client.IsConnected ? ConsoleColor.Green : ConsoleColor.Red), (4, _client.IsReceivingMessages ? ConsoleColor.Green : ConsoleColor.Red));
            composer.Write();
        }

        private static void ClearAnswerGuide()
        {
            var composer = _console.Compose();
            composer.Indent();
            var guide = "Press [C] to confirm.";
            composer.BeginWrite(new string(' ', guide.Length)).Run();
            composer.Console.CursorLeft = 0;
        }
    }
}