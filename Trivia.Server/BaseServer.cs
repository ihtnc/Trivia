namespace Trivia.Server
{
    internal class ServerLogEventArgs(string category, string message) : EventArgs
    {
        public string Category { get; } = category;
        public string Message { get; } = message;
    }

    internal class ServerErrorEventArgs(string category, Exception ex) : EventArgs
    {
        public string Category { get; } = category;
        public Exception Exception { get; } = ex;
    }

    internal class NewServerActionEventArgs(ServerAction action) : EventArgs
    {
        public ServerAction Action { get; } = action;
    }

    internal interface IServer : IDisposable
    {
        /// <summary>
        /// Starts the server.
        /// </summary>
        abstract CancellationTokenSource Start();

        /// <summary>
        /// Stops the server.
        /// </summary>
        abstract void Stop();

        /// <summary>
        /// Pings the server to check if it is running.
        /// </summary>
        abstract bool Ping();
    }

    /// <summary>
    /// Represents the server for the trivia game.
    /// </summary>
    internal abstract class BaseServer : IServer
    {
        /// <summary>
        /// Occurs when the server logs an event.
        /// </summary>
        public abstract event EventHandler<ServerLogEventArgs>? OnServerLog;

        /// <summary>
        /// Occurs when the server encounters an error.
        /// </summary>
        public abstract event EventHandler<ServerErrorEventArgs>? OnServerError;

        /// <summary>
        /// Occurs when a new server action is created.
        /// </summary>
        public abstract event EventHandler<NewServerActionEventArgs>? OnNewServerAction;

        public abstract CancellationTokenSource Start();

        public abstract void Stop();

        public abstract bool Ping();

        public abstract void Dispose();
    }
}