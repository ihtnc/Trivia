using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Trivia.Common;
using Trivia.Server.TriviaGame;

namespace Trivia.Server
{
    /// <summary>
    /// Represents the connection server for a trivia game. This server listens on two ports: one for sending messages to clients and another for receiving messages from clients.
    /// </summary>
    internal class ConnectionServer(TcpListener sender, TcpListener receiver) : BaseServer
    {
        /// <summary>
        /// The sender listener. This listener is used to send messages to clients. Connections to the sender listener are expected to be long-lived.
        /// </summary>
        private readonly TcpListener _sender = sender;

        /// <summary>
        /// The receiver listener. This listener is used to receive messages from clients. Connections to the receiver listener are expected to be short-lived.
        /// </summary>
        private readonly TcpListener _receiver = receiver;

        /// <summary>
        /// The Id assigned to the next client.
        /// </summary>
        private int _clientId = 0;

        private Task _incomingMessageTask = Task.CompletedTask;
        private Task _establishConnectionTask = Task.CompletedTask;

        /// <summary>
        /// The list of clients that the sender listener is expecting to connect with.
        /// </summary>
        private readonly ConcurrentDictionary<int, string> _expectedConnections = new();

        public ICollection<ITriviaClient> Clients => _clients.Values;

        /// <summary>
        /// The list of clients that have connected to the sender listener.
        /// </summary>
        private readonly ConcurrentDictionary<int, ITriviaClient> _clients = new();

        public override event EventHandler<ServerLogEventArgs>? OnServerLog;
        public override event EventHandler<ServerErrorEventArgs>? OnServerError;
        public override event EventHandler<NewServerActionEventArgs>? OnNewServerAction;

        /// <summary>
        /// The endpoint of the server. This is the receiver listener where clients are expected to send messages to.
        /// </summary>
        public IPEndPoint ServerEndPoint { get; private set; } = new IPEndPoint(IPAddress.None, 0);

        private CancellationTokenSource _cts = new();
        private CancellationToken _cancellationToken = CancellationToken.None;

        /// <summary>
        /// Starts the server.
        /// </summary>
        public override CancellationTokenSource Start()
        {
            _cts.Cancel();
            _cts = new CancellationTokenSource();
            _cancellationToken = _cts.Token;

            _sender.Start();
            _receiver.Start();

            _incomingMessageTask = Task.Run(() => HandleIncomingMessageAsync(_receiver, _cancellationToken), _cancellationToken);
            _establishConnectionTask = Task.Run(() => HandleEstablishConnectionAsync(_sender, _cancellationToken), _cancellationToken);

            ServerEndPoint = (IPEndPoint)_receiver.LocalEndpoint!;
            OnServerLog?.Invoke(this, new ServerLogEventArgs("Start", "Server started"));

            return _cts;
        }

        /// <summary>
        /// Stops the server.
        /// </summary>
        public override void Stop()
        {
            _cts.Cancel();
            _sender.Stop();
            _receiver.Stop();

            OnServerLog?.Invoke(this, new ServerLogEventArgs("Stop", "Server stopped"));
        }

        /// <summary>
        /// Pings the server to check if it is running.
        /// </summary>
        public override bool Ping()
        {
            var incomingMessageTaskIsActive = _incomingMessageTask.Status != TaskStatus.Canceled
                && _incomingMessageTask.Status != TaskStatus.Faulted
                && _incomingMessageTask.Status != TaskStatus.RanToCompletion;

            var establishConnectionTaskIsActive = _establishConnectionTask.Status != TaskStatus.Canceled
                && _establishConnectionTask.Status != TaskStatus.Faulted
                && _establishConnectionTask.Status != TaskStatus.RanToCompletion;

            var tasksAreActive = incomingMessageTaskIsActive && establishConnectionTaskIsActive;
            if (tasksAreActive)
            {
                OnServerLog?.Invoke(this, new ServerLogEventArgs("Ping", "Tasks are running"));
            }
            else
            {
                OnServerError?.Invoke(this, new ServerErrorEventArgs("Ping", new Exception("Tasks are not running")));
            }

            return tasksAreActive;
        }

        /// <summary>
        /// Handles incoming messages from clients. Connections to the receiver listener are closed after processing.
        /// </summary>
        /// <param name="listener">The receiver listener that will accept clients.</param>
        private async Task HandleIncomingMessageAsync(TcpListener listener, CancellationToken cancellationToken = default)
        {
            while (true)
            {
                if (cancellationToken.IsCancellationRequested) { return; }

                var newClient = await listener.AcceptTcpClientAsync(cancellationToken);

                _ = Task.Run(async () =>
                {
                    TcpClient? client = newClient;
                    try
                    {
                        var message = await client.ReadFromClientAsync(wait: true, cancellationToken: cancellationToken);
                        if (message == null) { return; }

                        OnServerLog?.Invoke(this, new ServerLogEventArgs($"{message.MessageType}", $"Received from {client.Client.RemoteEndPoint!}"));

                        var response = HandleClientMessage(message);
                        await client.SendToClientAsync(response, cancellationToken);

                        OnServerLog?.Invoke(this, new ServerLogEventArgs($"{message.MessageType}", $"Accepted {client.Client.RemoteEndPoint!}"));
                    }
                    catch (Exception ex)
                    {
                        OnServerError?.Invoke(this, new ServerErrorEventArgs($"{client.Client.RemoteEndPoint!}", ex));
                    }
                    finally
                    {
                        client?.Close();
                    }
                }, cancellationToken);
            }
        }

        /// <summary>
        /// Handles establishing a client connection to the sender listener. This method expects the client to send an establish connection message to the sender listener.
        /// This is a one-time message to the sender listener since succeeding client messages are expected to be sent to the receiver listener.
        /// </summary>
        /// <param name="listener">The sender listener that will accept clients.</param>
        private async Task HandleEstablishConnectionAsync(TcpListener listener, CancellationToken cancellationToken = default)
        {
            while (true)
            {
                if (cancellationToken.IsCancellationRequested) { return; }

                var newClient = await listener.AcceptTcpClientAsync(cancellationToken);

                _ = Task.Run(async () =>
                {
                    var client = newClient;

                    try
                    {
                        var message = await client.ReadEstablishConnectionFromClientAsync(wait: true, cancellationToken: cancellationToken);
                        if (message == null
                            || !_expectedConnections.ContainsKey(message.ClientId)
                            || _expectedConnections[message.ClientId] != message.Name
                            || ClientExist(message.ClientId, message.Name))
                        {
                            await client.SendErrorToClientAsync("Invalid request", cancellationToken);
                            OnServerLog?.Invoke(this, new ServerLogEventArgs($"{TcpServerMessageType.Error}", $"Invalid request from {client.Client.RemoteEndPoint!}"));
                            client.Close();
                            return;
                        }

                        await client.SendAcceptedToClientAsync(cancellationToken);

                        _expectedConnections.TryRemove(message.ClientId, out _);

                        var participant = new TriviaParticipant(message.ClientId, message.Name, client);
                        _clients.TryAdd(message.ClientId, participant);

                        var action = new AddTriviaParticipantServerAction(participant);
                        OnNewServerAction?.Invoke(this, new NewServerActionEventArgs(action));

                        OnServerLog?.Invoke(this, new ServerLogEventArgs($"{message.MessageType}", $"Accepted {client.Client.RemoteEndPoint!}"));
                    }
                    catch (Exception ex)
                    {
                        OnServerError?.Invoke(this, new ServerErrorEventArgs($"{client.Client.RemoteEndPoint!}", ex));
                        client.Close();
                    }
                 }, cancellationToken);
            }
        }

        /// <summary>
        /// Handles the client message.
        /// </summary>
        /// <param name="message">The message to handle.</param>
        /// <returns>The server message to be sent as response to the client message.</returns>
        private TcpServerMessage HandleClientMessage(TcpClientMessage message)
        {
            switch (message.MessageType)
            {
                case TcpClientMessageType.Request:
                    return HandleRequestConnection(message.ToRequestConnection());

                case TcpClientMessageType.Disconnect:
                    return HandleDisconnect(message.ToDisconnect());

                case TcpClientMessageType.TriviaAnswer:
                    return HandleTriviaAnswer(message.ToTriviaAnswer());

                case TcpClientMessageType.RequestRoundDetails:
                    return HandleRequestRoundDetails(message.ToRequestRoundDetails());

                default:
                    var error = ErrorServerMessage.Create("Invalid message type");
                    return error;
            }
        }

        /// <summary>
        /// Handles a request connection message.
        /// </summary>
        /// <param name="message">The request connection message.</param>
        /// <returns>
        /// The server message to be sent as response to the client message.
        /// </returns>
        private TcpServerMessage HandleRequestConnection(RequestConnectionClientMessage? message)
        {
            if (message == null)
            {
                var error = ErrorServerMessage.Create("Invalid payload");
                return error;
            }

            var clientId = ++_clientId;
            _expectedConnections.TryAdd(clientId, message.Name);

            var sender = (IPEndPoint)_sender.LocalEndpoint!;
            var response = SetupConnectionServerMessage.Create(clientId, sender);
            return response;
        }

        /// <summary>
        /// Handles a disconnect message.
        /// </summary>
        /// <param name="message">The disconnect message.</param>
        /// <returns>
        /// The server message to be sent as response to the client message.
        /// </returns>
        private TcpServerMessage HandleDisconnect(DisconnectClientMessage? message)
        {
            if (message == null || !ClientExist(message.ClientId, message.Name))
            {
                var error = ErrorServerMessage.Create("Invalid payload");
                return error;
            }

            _clients.TryRemove(message.ClientId, out var client);
            client?.Client.Close();

            var action = new RemoveTriviaParticipantServerAction(message.ClientId);
            OnNewServerAction?.Invoke(this, new NewServerActionEventArgs(action));

            var response = AcceptedServerMessage.Create();
            return response;
        }

        /// <summary>
        /// Handles a trivia answer message.
        /// </summary>
        /// <param name="message">The trivia answer message.</param>
        /// <returns>
        /// The server message to be sent as response to the client message.
        /// </returns>
        private TcpServerMessage HandleTriviaAnswer(TriviaAnswerClientMessage? message)
        {
            if (message == null || !ClientExist(message.ClientId, message.Name))
            {
                var error = ErrorServerMessage.Create("Invalid payload");
                return error;
            }

            var answer = new TriviaRoundAnswer
            {
                RoundId = message.RoundId,
                QuestionId = message.QuestionId,
                ClientId = message.ClientId,
                Answer = message.AnswerIndex
            };
            var action = new SetTriviaAnswerServerAction(answer);
            OnNewServerAction?.Invoke(this, new NewServerActionEventArgs(action));

            var response = AcceptedServerMessage.Create();
            return response;
        }

        /// <summary>
        /// Handles a request round details message
        /// </summary>
        /// <param name="message">The request round details mesasge.</param>
        /// <returns>
        /// The server message to be sent as response to the client message.
        /// </returns>
        private TcpServerMessage HandleRequestRoundDetails(RequestRoundDetailsClientMessage? message)
        {
            if (message == null || !ClientExist(message.ClientId, message.Name))
            {
                var error = ErrorServerMessage.Create("Invalid payload");
                return error;
            }

            var action = new RequestRoundDetailsServerAction(message.ClientId);
            OnNewServerAction?.Invoke(this, new NewServerActionEventArgs(action));

            var response = AcceptedServerMessage.Create();
            return response;
        }

        /// <summary>
        /// Sends a message to a client.
        /// </summary>
        public void SendMessage(int clientId, TcpServerMessage message, CancellationToken cancellationToken = default)
        {
            if (!_clients.TryGetValue(clientId, out ITriviaClient? value)) { return; }

            var client = value.Client;
            if (!client.Connected) { return; }

            _ = Task.Run(async () => await client.SendToClientAsync(message, cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Checks if a client exists.
        /// </summary>
        private bool ClientExist(int clientId, string name)
        {
            return _clients.ContainsKey(clientId) && _clients[clientId].Name.Equals(name, StringComparison.OrdinalIgnoreCase);
        }

        public override void Dispose()
        {
            _cts.Dispose();

            _sender.Dispose();
            _receiver.Dispose();

            _incomingMessageTask.Dispose();
            _establishConnectionTask.Dispose();
        }
    }
}