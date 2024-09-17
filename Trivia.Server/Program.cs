using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Trivia.Common;
using Trivia.Server.TriviaApiClient;
using Trivia.Server.TriviaGame;

namespace Trivia.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var hostBuilder = Host.CreateApplicationBuilder(args);
            hostBuilder.Services.AddTransient<IApiClient, ApiClient>();
            hostBuilder.Services.AddTransient<ITriviaProvider, TriviaProvider>();
            hostBuilder.Services.AddTransient<ITriviaRound, TriviaRound>();
            hostBuilder.Services.AddTransient<IConsole, TriviaConsole>();
            hostBuilder.Services.AddTransient<ITriviaConsoleComposer, TriviaConsoleComposer>();
            using var host = hostBuilder.Build();

            var server = new TriviaServer(host.Services);
            server.Run();
        }
    }
}