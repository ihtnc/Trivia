using System.Collections.Concurrent;
using Trivia.Common;
using Trivia.Server.TriviaGame;

namespace Trivia.Server
{
    internal class AddTriviaParticipantEventArgs(TriviaParticipant participant) : EventArgs
    {
        public TriviaParticipant Participant { get; } = participant;
    }

    internal class RemoveTriviaParticipantEventArgs(int clientId) : EventArgs
    {
        public int ClientId { get; } = clientId;
    }

    internal class SetTriviaAnswerEventArgs(TriviaRoundAnswer answer) : EventArgs
    {
        public TriviaRoundAnswer Answer { get; } = answer;
    }

    internal class SendClientMessageEventArgs(int clientId, TcpServerMessage message) : EventArgs
    {
        public int ClientId { get; } = clientId;
        public TcpServerMessage Message { get; } = message;
    }

    internal class RequestRoundDetailsEventArgs(int clientId) : EventArgs
    {
        public int ClientId { get; } = clientId;
    }

    /// <summary>
    /// Represents a server for a trivia game. This server listens on two ports: one for sending messages to clients and another for receiving messages from clients.
    /// </summary>
    internal class ActionServer : BaseServer
    {
        /// <summary>
        /// The queue of server actions to perform.
        /// </summary>
        public ConcurrentQueue<ServerAction> Queue { get; } = new();

        private Task _performActionTask = Task.CompletedTask;

        public override event EventHandler<ServerLogEventArgs>? OnServerLog;
        public override event EventHandler<ServerErrorEventArgs>? OnServerError;
        public override event EventHandler<NewServerActionEventArgs>? OnNewServerAction;

        /// <summary>
        /// Occurs when a new trivia participant intends to join the server.
        /// </summary>
        public event EventHandler<AddTriviaParticipantEventArgs>? OnAddTriviaParticipant;

        /// <summary>
        /// Occurs when a trivia participant intends to leave the server.
        /// </summary>
        public event EventHandler<RemoveTriviaParticipantEventArgs>? OnRemoveTriviaParticipant;

        /// <summary>
        /// Occurs when a trivia participant answers a question.
        /// </summary>
        public event EventHandler<SetTriviaAnswerEventArgs>? OnSetTriviaAnswer;

        /// <summary>
        /// Occurs when a message is sent to a client.
        /// </summary>
        public event EventHandler<SendClientMessageEventArgs>? OnSendClientMessage;

        /// <summary>
        /// Occurs when a trivia participants requests round details
        /// </summary>
        public event EventHandler<RequestRoundDetailsEventArgs>? OnRequestRoundDetails;

        /// <summary>
        /// Occurs when the game is moved to the next state.
        /// </summary>
        public event EventHandler? OnMoveGameToNextState;

        private CancellationTokenSource _cts = new();
        private CancellationToken _cancellationToken = CancellationToken.None;

        /// <summary>
        /// Starts the server.
        /// </summary>
        public override CancellationTokenSource Start()
        {
            _cts = new CancellationTokenSource();
            _cancellationToken = _cts.Token;

            _performActionTask = Task.Run(PerformServerAction, _cancellationToken);

            OnServerLog?.Invoke(this, new ServerLogEventArgs("Start", "Server started"));
            return _cts;
        }

        /// <summary>
        /// Stops the server.
        /// </summary>
        public override void Stop()
        {
            _cts.Cancel();
            OnServerLog?.Invoke(this, new ServerLogEventArgs("Stop", "Server stopped"));
        }

        /// <summary>
        /// Pings the server to check if it is running.
        /// </summary>
        public override bool Ping()
        {
            var tasksAreActive = _performActionTask.Status != TaskStatus.Canceled
                && _performActionTask.Status != TaskStatus.Faulted
                && _performActionTask.Status != TaskStatus.RanToCompletion;
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
        /// Performs server actions.
        /// </summary>
        /// <param name="listener">The sender listener for sending messages to clients in case needed.</param>
        private async Task PerformServerAction()
        {
            while (true)
            {
                if (_cancellationToken.IsCancellationRequested) { return; }

                if (!Queue.TryDequeue(out var action))
                {
                    await Task.Delay(100, _cancellationToken);
                    continue;
                }

                HandleServerActionAsync(action);
            }
        }

        /// <summary>
        /// Handles server actions.
        /// </summary>
        /// <param name="action">The server action to handle.</param>
        private void HandleServerActionAsync(ServerAction action)
        {
            switch (action)
            {
                case AddTriviaParticipantServerAction addAction:
                    OnAddTriviaParticipant?.Invoke(this, new AddTriviaParticipantEventArgs(addAction.Participant));
                    break;

                case RemoveTriviaParticipantServerAction removeAction:
                    OnRemoveTriviaParticipant?.Invoke(this, new RemoveTriviaParticipantEventArgs(removeAction.ClientId));
                    break;

                case SetTriviaAnswerServerAction answerAction:
                    OnSetTriviaAnswer?.Invoke(this, new SetTriviaAnswerEventArgs(answerAction.Answer));
                    break;

                case SendClientMessageServerAction messageAction:
                    OnSendClientMessage?.Invoke(this, new SendClientMessageEventArgs(messageAction.ClientId, messageAction.Message));
                    break;

                case MoveGameToNextStateServerAction nextStateAction:
                    OnMoveGameToNextState?.Invoke(this, EventArgs.Empty);
                    break;

                case RequestRoundDetailsServerAction requestRoundDetailsAction:
                    OnRequestRoundDetails?.Invoke(this, new RequestRoundDetailsEventArgs(requestRoundDetailsAction.ClientId));
                    break;
            }
        }

        public override void Dispose()
        {
            _cts.Dispose();
            _performActionTask.Dispose();
        }
    }
}