using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Trivia.Common;
using Trivia.Server.TriviaGame;

namespace Trivia.Server
{
    public class TriviaServer : IDisposable
    {
        /// <summary>
        /// The list of trivia categories.
        /// </summary>
        private IDictionary<string, TriviaCategory> _categories = new Dictionary<string, TriviaCategory>();

        private ConnectionServer? _connectionServer;
        private ActionServer? _actionServer;
        private GameServer? _gameServer;

        private CancellationTokenSource _connectionCts = new();
        private CancellationTokenSource _actionCts = new();
        private CancellationTokenSource _gameCts = new();

        private readonly IServiceProvider _serviceProvider;
        private readonly IConsole _console;

        public TriviaServer(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _console = _serviceProvider.GetRequiredService<IConsole>();
        }

        public void Run()
        {
            _console.Clear();

            try
            {
                // get the current ip address
                IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());
                IPAddress ipAddress = localIPs.First(ip => ip.AddressFamily == AddressFamily.InterNetwork);

                var senderPort = GetAvailablePort();
                var sender = new TcpListener(ipAddress, senderPort);
                sender.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                var receiverPort = GetAvailablePort();
                var receiver = new TcpListener(ipAddress, receiverPort);
                receiver.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                _connectionServer = new ConnectionServer(sender, receiver);
                _connectionServer.OnServerLog += OnServerLog;
                _connectionServer.OnServerError += OnServerError;
                _connectionServer.OnNewServerAction += OnNewServerAction;
                _connectionCts = _connectionServer.Start();

                _actionServer = new ActionServer();
                _actionServer.OnServerLog += OnServerLog;
                _actionServer.OnServerError += OnServerError;
                _actionServer.OnAddTriviaParticipant += OnActionServerAddParticipant;
                _actionServer.OnRemoveTriviaParticipant += OnActionServerRemoveParticipant;
                _actionServer.OnSetTriviaAnswer += OnActionServerSetTriviaAnswer;
                _actionServer.OnSendClientMessage += OnActionServerSendClientMessage;
                _actionServer.OnMoveGameToNextState += OnActionServerMoveGameToNextState;
                _actionServer.OnRequestRoundDetails += OnActionServerRequestRoundDetails;
                _actionCts = _actionServer.Start();

                var triviaProvider = _serviceProvider.GetRequiredService<ITriviaProvider>();
                var triviaRound = _serviceProvider.GetRequiredService<ITriviaRound>();
                _gameServer = new GameServer(triviaProvider, triviaRound);
                _gameServer.OnServerLog += OnServerLog;
                _gameServer.OnServerError += OnServerError;
                _gameServer.OnNewServerAction += OnNewServerAction;
                _gameCts = _gameServer.Start();
                CacheCategories(_gameServer.Categories);

                DisplayMenu();

                string input = string.Empty;
                do
                {
                    input = string.Empty;

                    if (_console.KeyAvailable)
                    {
                        input = $"{_console.ReadKey(true).KeyChar}";
                    }

                    if (input.Length == 0) { continue; }
                    HandleInput(input);

                } while (!input.Equals("Q", StringComparison.OrdinalIgnoreCase));

                _connectionServer.Stop();
                _actionServer.Stop();
                _gameServer.Stop();
            }
            catch (Exception ex)
            {
                LogError("TriviaServer", $"{ex}");
            }
            finally
            {
                _connectionCts.Cancel();
                _actionCts.Cancel();
                _gameCts.Cancel();

                Log("TriviaServer", "Servers stopped.");

                var composer = _console.Compose();
                composer.AddLine();
                composer.AddLine("Press any key to exit...");
                composer.Write();

                _console.ReadKey(true);
            }
        }

        /// <summary>
        /// Handles the input from the console.
        /// </summary>
        /// <param name="input">The input from the console.</param>
        /// <param name="connectionServer">The connection server.</param>
        /// <param name="actionServer">The action server.</param>
        /// <param name="gameServer">The game server.</param>
        internal void HandleInput(string input)
        {
            switch (input.ToUpper())
            {
                case "P":
                    Ping(_connectionServer, _actionServer, _gameServer);
                    break;
                case "U":
                    Ping(_connectionServer);
                    break;
                case "I":
                    Ping(_actionServer);
                    break;
                case "O":
                    Ping(_gameServer);
                    break;
                case "C":
                    RestartServer(_connectionServer);
                    break;
                case "A":
                    RestartServer(_actionServer);
                    break;
                case "G":
                    RestartServer(_gameServer);
                    break;
                case "J":
                    ViewConnections();
                    break;
                case "K":
                    ViewQueue();
                    break;
                case "L":
                    ViewGame();
                    break;

                case "V":
                    ViewCategories();
                    break;
                case "0":
                case "1":
                case "2":
                case "3":
                case "4":
                case "5":
                case "6":
                case "7":
                case "8":
                case "9":
                    ChangeCategory(input);
                    break;
                case "R":
                    ChangeDifficulty();
                    break;
                case "M":
                    IncreaseNumberOfQuestions();
                    break;
                case "N":
                    DecreaseNumberOfQuestions();
                    break;

                case "Q":
                    break;

                default:
                    DisplayMenu();
                    break;
            }
        }

        internal void OnActionServerAddParticipant(object? source, AddTriviaParticipantEventArgs e)
        {
            _gameServer?.AddParticipant(e.Participant);
        }

        internal void OnActionServerRemoveParticipant(object? source, RemoveTriviaParticipantEventArgs e)
        {
            _gameServer?.RemoveParticipant(e.ClientId);
        }

        internal void OnActionServerSetTriviaAnswer(object? source, SetTriviaAnswerEventArgs e)
        {
            _gameServer?.SetParticipantAnswer(e.Answer.ClientId, e.Answer);
        }

        internal void OnActionServerSendClientMessage(object? source, SendClientMessageEventArgs e)
        {
            _connectionServer?.SendMessage(e.ClientId, e.Message);
        }

        internal void OnActionServerMoveGameToNextState(object? source, EventArgs e)
        {
            _gameServer?.MoveToNextState();
        }

        internal void OnActionServerRequestRoundDetails(object? source, RequestRoundDetailsEventArgs e)
        {
            _gameServer?.ProvideRoundDetailsToParticipant(e.ClientId);
        }

        internal void OnServerLog(object? source, ServerLogEventArgs e)
        {
            if (source is ConnectionServer)
            {
                LogConnectionServer(e.Category, e.Message);
            }
            else if (source is ActionServer)
            {
                LogActionServer(e.Category, e.Message);
            }
            else if (source is GameServer)
            {
                LogGameServer(e.Category, e.Message);
            }
            else
            {
                Log("TriviaServer", $"{e.Category}: {e.Message}", ConsoleColor.DarkGray);
            }
        }

        internal void OnServerError(object? source, ServerErrorEventArgs e)
        {
            if (source is ConnectionServer)
            {
                LogError("ConnectionServer", $"{e.Category}: {e.Exception}");
            }
            else if (source is ActionServer)
            {
                LogError("ActionServer", $"{e.Category}: {e.Exception}");
            }
            else if (source is GameServer)
            {
                LogError("GameServer", $"{e.Category}: {e.Exception}");
            }
            else
            {
                LogError("TriviaServer", $"{e.Category}: {e.Exception}");
            }
        }

        internal void OnNewServerAction(object? source, NewServerActionEventArgs e)
        {
            _actionServer?.Queue.Enqueue(e.Action);

            if (source is ConnectionServer)
            {
                LogConnectionServer($"{e.Action.ActionType}", "Action added");
            }
            else if (source is GameServer)
            {
                LogGameServer($"{e.Action.ActionType}", "Action added");
            }
            else
            {
                var log = $"{e.Action.ActionType}: Action added";
                Log("TriviaServer", log, ConsoleColor.DarkGray);
            }
        }

        internal void LogConnectionServer(string category, string message)
        {
            var color = category.Equals("Error", StringComparison.OrdinalIgnoreCase) ? ConsoleColor.Red : ConsoleColor.DarkGray;
            var triviaLog = $"{category}: {message}";
            Log("ConnectionServer", triviaLog, color);
        }

        internal void LogActionServer(string category, string message)
        {
            var color = category.Equals("Error", StringComparison.OrdinalIgnoreCase) ? ConsoleColor.Red : ConsoleColor.DarkGray;
            var actionLog = $"{category}: {message}";
            Log("ActionServer", actionLog, color);
        }

        internal void LogGameServer(string category, string message)
        {
            var color = ConsoleColor.DarkGray;

            if (category.Equals("Error", StringComparison.OrdinalIgnoreCase)) { color = ConsoleColor.Red; }
            if (category.Equals("Option", StringComparison.OrdinalIgnoreCase)) { color = ConsoleColor.Yellow; }
            if (category.Equals("Game", StringComparison.OrdinalIgnoreCase)) { color = _console.ForegroundColor; }

            var gameLog = $"{category}: {message}";
            Log("GameServer", gameLog, color);
        }

        internal void LogError(string source, string message)
        {
            var error = $"[ERR]({source}): {message}";
            Log(error, ConsoleColor.Red);
        }

        internal void LogWarning(string source, string message)
        {
            var warning = $"[WRN]({source}): {message}";
            Log(warning, ConsoleColor.Yellow);
        }

        internal void Log(string source, string message, ConsoleColor? color = null)
        {
            var log = $"[LOG]({source}): {message}";
            Log(log, color);
        }

        internal void Log(string message, ConsoleColor? color = null)
        {
            if (color == null)
            {
                _console.WriteLine(message);
                return;
            }

            _console.WriteLine(message, color.Value);
        }

        private void CacheCategories(IReadOnlyCollection<TriviaCategory> categories)
        {
            _categories = categories.Where(IsCategoryAllowed)
                .Select((c, index) => (c, index + 1))
                .ToDictionary(k => $"{k.Item2}", v => v.c);
        }

        private bool IsCategoryAllowed(TriviaCategory category)
        {
            return category.Id == 9  // General Knowledge
                || category.Id == 17 // Science & Nature
                || category.Id == 23 // History
                || category.Id == 22 // Geography
                || category.Id == 11 // Film
                || category.Id == 25 // Art
                || category.Id == 21 // Sports
                || category.Id == 18 // Science: Computers
                || category.Id == 15 // Video Games
                ;
        }

        private void DisplayMenu()
        {
            var composer = _console.Compose();
            composer.AddLine();
            var headerLine = composer.AddLine("Main Menu (Server: ", $"{_connectionServer?.ServerEndPoint}", ")")
                .WithColors((1, ConsoleColor.Green));
            var headerLength = headerLine.RawString.Length;
            composer.InsertLine(1, new string('=', headerLength));
            composer.AddLine(new string('=', headerLength));
            composer.AddLine();
            composer.Write();

            composer.AddLine("Connection server", ": [", "U", "]=Ping [", "C", "]=Restart [", "J", "]=View connections")
                .WithColors((2, ConsoleColor.Yellow), (4, ConsoleColor.Yellow), (6, ConsoleColor.Yellow));
            composer.AddLine("Action server", ": [", "I", "]=Ping [", "A", "]=Restart [", "K", "]=View queue")
                .WithColors((2, ConsoleColor.Yellow), (4, ConsoleColor.Yellow), (6, ConsoleColor.Yellow));
            composer.AddLine("Game server", ": [", "O", "]=Ping [", "G", "]=Restart [", "L", "]=View round")
                .WithColors((2, ConsoleColor.Yellow), (4, ConsoleColor.Yellow), (6, ConsoleColor.Yellow));
            composer.AddLine();
            composer.AlignLeft(0);
            composer.Write();

            composer.AddLine("Next Round");
            composer.AddLine("Category (difficulty): ", string.IsNullOrWhiteSpace(_gameServer?.Category) ? "Any" : _gameServer.Category, " (", $"{_gameServer?.Difficulty}", ")")
                .WithColors((1, ConsoleColor.Yellow), (3, ConsoleColor.Yellow));
            composer.AddLine("Questions: ", _gameServer?.NumberOfQuestions ?? 0)
                .WithColors((1, ConsoleColor.Yellow));
            composer.AddLine();
            composer.Write();

            composer.AddLine("Category", ": [", "V", "]=View list [", "0", "-", _categories.Count, "]=Set category")
                .WithColors((2, ConsoleColor.Yellow), (4, ConsoleColor.Yellow), (6, ConsoleColor.Yellow));
            composer.AddLine("Difficulty", ": [", "R", "]=Change")
                .WithColors((2, ConsoleColor.Yellow));
            composer.AddLine("Questions", ": [", "M", "]=Increase [", "N", "]=Decrease")
                .WithColors((2, ConsoleColor.Yellow), (4, ConsoleColor.Yellow));
            composer.AddLine();
            composer.AlignLeft(0);
            composer.Write();

            composer.AddLine("[", "P", "]=Ping servers")
                .WithColors((1, ConsoleColor.Yellow));
            composer.AddLine();
            composer.AddLine("[", "Q", "]=Quit")
                .WithColors((1, ConsoleColor.Yellow));
            composer.AddLine();
            composer.Write();

            DisplayPressAKey();
        }

        private void DisplayPressAKey()
        {
            _console.WriteLine("Press a key", ConsoleColor.DarkGray);
        }

        private static void Ping(params BaseServer?[] servers)
        {
            foreach (var server in servers)
            {
                server?.Ping();
            }
        }

        private static void RestartServer(BaseServer? server)
        {
            server?.Stop();
            server?.Start();
        }

        private void ViewConnections()
        {
            _console.BeginWriteLine("Connections: ", _connectionServer?.Clients.Count ?? 0)
                .WithColors((1, ConsoleColor.Yellow))
                .Run();
        }

        private void ViewQueue()
        {
            _console.BeginWriteLine("Queue: ", _actionServer?.Queue.Count ?? 0)
                .WithColors((1, ConsoleColor.Yellow))
                .Run();
        }

        private void ViewGame()
        {
            var round = _gameServer?.Round;
            var roundStarted = round?.IsRoundStarted == true;
            var roundOver = round?.IsRoundOver == true;

            var composer = _console.Compose();
            var status = _gameServer != null ? $"{_gameServer.State}" : "Unknown";
            composer.AddLine("Round: ", round?.RoundId ?? 0, " (", status, ")")
                .WithColors((1, ConsoleColor.Yellow), (3, ConsoleColor.Yellow));

            var category = roundStarted ? round?.Category?.Category : _gameServer?.Category;
            var difficulty = roundStarted ? round?.Difficulty : _gameServer?.Difficulty;
            composer.AddLine("Category (Difficulty): ", string.IsNullOrWhiteSpace(category) ? "Any" : category, " (", $"{difficulty}", ")")
                .WithColors((1, ConsoleColor.Yellow), (3, ConsoleColor.Yellow));

            var numberOfQuestions = roundStarted ? round?.NumberOfQuestions ?? 0 : _gameServer?.NumberOfQuestions ?? 0;
            if (roundStarted && !roundOver)
            {
                composer.AddLine("Questions: ", round?.CurrentQuestion?.QuestionId ?? 0, " of ", numberOfQuestions)
                    .WithColors((1, ConsoleColor.Yellow), (3, ConsoleColor.Yellow));
            }
            else
            {
                composer.AddLine("Questions: ", numberOfQuestions)
                    .WithColors((1, ConsoleColor.Yellow));
            }

            var participants = roundStarted ? round?.ParticipantClientIds.Count ?? 0 : _gameServer?.Participants.Count ?? 0;
            composer.AddLine("Participants: ", participants)
                .WithColors((1, ConsoleColor.Yellow));

            composer.Write();
        }

        private void ViewCategories()
        {
            var composer = _console.Compose();
            composer.AddLine("Trivia Categories: ");

            foreach (var category in _categories)
            {
                composer.AddLine("[", category.Key, "]", " ", category.Value.Category)
                    .WithColors((1, ConsoleColor.Yellow));
            }

            composer.AddLine("[", "0", "]", " Any")
                .WithColors((1, ConsoleColor.Yellow));

            composer.Write();
        }

        private void ChangeCategory(string displayId)
        {
            if (displayId.Equals("0", StringComparison.OrdinalIgnoreCase))
            {
                _gameServer?.SetCategory(string.Empty);
            }
            else if (_categories.TryGetValue(displayId, out TriviaCategory? value))
            {
                _gameServer?.SetCategory(value.Category);
            }
        }

        private void ChangeDifficulty()
        {
            var difficulty = _gameServer?.Difficulty;

            switch(difficulty)
            {
                case TriviaDifficulty.Any:
                    _gameServer?.SetDifficulty(TriviaDifficulty.Easy);
                    break;
                case TriviaDifficulty.Easy:
                    _gameServer?.SetDifficulty(TriviaDifficulty.Medium);
                    break;
                case TriviaDifficulty.Medium:
                    _gameServer?.SetDifficulty(TriviaDifficulty.Hard);
                    break;
                case TriviaDifficulty.Hard:
                    _gameServer?.SetDifficulty(TriviaDifficulty.Any);
                    break;
                default:
                    _gameServer?.SetDifficulty(TriviaDifficulty.Any);
                    break;
            }
        }

        private void IncreaseNumberOfQuestions()
        {
            var number = _gameServer?.NumberOfQuestions ?? 0;
            _gameServer?.SetNumberOfQuestions(number + 1);
        }

        private void DecreaseNumberOfQuestions()
        {
            var number = _gameServer?.NumberOfQuestions ?? 0;
            _gameServer?.SetNumberOfQuestions(number - 1);
        }

        private static readonly IPEndPoint DefaultLoopbackEndpoint = new(IPAddress.Loopback, port: 0);
        private static int GetAvailablePort()
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(DefaultLoopbackEndpoint);
            return ((IPEndPoint)socket.LocalEndPoint!).Port;
        }

        public void Dispose()
        {
            _connectionCts.Dispose();
            _actionCts.Dispose();
            _gameCts.Dispose();

            _connectionServer?.Dispose();
            _actionServer?.Dispose();
            _gameServer?.Dispose();
        }
    }
}