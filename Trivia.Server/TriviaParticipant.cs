using System.Net.Sockets;

namespace Trivia.Server
{
    internal interface ITriviaClient
    {
        /// <summary>
        /// The Id assigned to the client.
        /// </summary>
        int ClientId { get; }

        /// <summary>
        /// The name of the participant.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The client associated with the participant.
        /// </summary>
        TcpClient Client { get; }
    }

    internal interface ITriviaParticipant
    {
        /// <summary>
        /// The Id assigned to the client.
        /// </summary>
        int ClientId { get; }

        /// <summary>
        /// The name of the participant.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The score of the participant.
        /// </summary>
        int Score { get; }

        /// <summary>
        /// Adds a point to the score of the participant.
        /// </summary>
        void AddPoint();
    }

    /// <summary>
    /// Represents a participant in a trivia game.
    /// </summary>
    internal class TriviaParticipant(int clientId, string name, TcpClient client) : ITriviaParticipant, ITriviaClient
    {
        /// <summary>
        /// The Id assigned to the client.
        /// </summary>
        public int ClientId { get; } = clientId;

        /// <summary>
        /// The name of the participant.
        /// </summary>
        public string Name { get; } = name;

        /// <summary>
        /// The client associated with the participant.
        /// </summary>
        public TcpClient Client { get; } = client;

        /// <summary>
        /// The score of the participant.
        /// </summary>
        public int Score { get; private set; } = 0;

        /// <summary>
        /// Adds a point to the score of the participant.
        /// </summary>
        public void AddPoint()
        {
            Score++;
        }
    }
}