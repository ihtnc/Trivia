using Trivia.Common;
using Trivia.Server.TriviaGame;

namespace Trivia.Server
{
    internal enum ServerActionType
    {
        AddParticipant,
        RemoveParticipant,
        SetAnswer,
        SendClientMessage,
        MoveGameToNextState,
        RequestRoundDetails
    }

    internal abstract class ServerAction
    {
        public abstract ServerActionType ActionType { get; }
    }

    /// <summary>
    /// Represents an action to add a trivia participant.
    /// </summary>
    /// <param name="clientId">The Id assigned to the client.</param>
    /// <param name="name">The name of the client.</param>
    /// <param name="client">The client's TCP connection.</param>
    internal class AddTriviaParticipantServerAction(TriviaParticipant participant) : ServerAction
    {
        public override ServerActionType ActionType => ServerActionType.AddParticipant;

        public TriviaParticipant Participant { get; } = participant;
    }

    /// <summary>
    /// Represents an action to disconnect a client from the server.
    /// </summary>
    /// <param name="clientId">The Id assigned to the client.</param>
    internal class RemoveTriviaParticipantServerAction(int clientId) : ServerAction
    {
        public override ServerActionType ActionType => ServerActionType.RemoveParticipant;

        public int ClientId { get; } = clientId;
    }

    /// <summary>
    /// Represents an action to set an answer for a question in a round.
    /// </summary>
    internal class SetTriviaAnswerServerAction(TriviaRoundAnswer answer) : ServerAction
    {
        public override ServerActionType ActionType => ServerActionType.SetAnswer;

        public TriviaRoundAnswer Answer { get; } = answer;
    }

    /// <summary>
    /// Represents an action to send a message to a client.
    /// </summary>
    internal class SendClientMessageServerAction(int clientId, TcpServerMessage message) : ServerAction
    {
        public override ServerActionType ActionType => ServerActionType.SendClientMessage;

        public int ClientId { get; } = clientId;
        public TcpServerMessage Message { get; } = message;
    }

    /// <summary>
    /// Represents an action to move the game to the next state.
    /// </summary>
    internal class MoveGameToNextStateServerAction : ServerAction
    {
        public override ServerActionType ActionType => ServerActionType.MoveGameToNextState;
    }

    /// <summary>
    /// Represents an action to request for round details
    /// </summary>
    internal class RequestRoundDetailsServerAction(int clientId) : ServerAction
    {
        public override ServerActionType ActionType => ServerActionType.RequestRoundDetails;

        public int ClientId { get; } = clientId;
    }
}