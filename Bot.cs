using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Serilog;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;

using CookingBot.Data;
using CookingBot.Commands;

namespace CookingBot
{
    public class Bot
    {
        private const int MaxConnectionAttempts = 5;
        private const int ConnectionAttemptDelay = 1000;
        private static bool StopFlag = false;

        private static bool ResetFlag = false;

        public static void SetStopFlag()
        {
            StopFlag = true;
        }

        DiscordShardedClient client;

        public async Task MainAsync(ILoggerFactory loggerFactory, string token)
        {
            int connectionAttempts = 1;

            while (true)
            {
                if (await DatabaseManager.GetConnectionStatus())
                {
                    break;
                }
                if (connectionAttempts > MaxConnectionAttempts)
                {
                    Log.Fatal("Unable to connect to database, shutting down.");
                    return;
                }
                else if (connectionAttempts == MaxConnectionAttempts)
                {
                    Log.Warning("Database connection could not be established! Attempt {CurrentAttempt}/{MaxAttempts}. Retrying in {RetryTime}S Final attempt", connectionAttempts, MaxConnectionAttempts, (ConnectionAttemptDelay / 1000) * connectionAttempts);
                }
                else
                {
                    Log.Warning("Database connection could not be established! Attempt {CurrentAttempt}/{MaxAttempts}. Retrying in {RetryTime}S", connectionAttempts, MaxConnectionAttempts, (ConnectionAttemptDelay / 1000) * connectionAttempts);
                }
                await Task.Delay(ConnectionAttemptDelay * connectionAttempts);
                connectionAttempts++;
            }

            var services = new ServiceCollection()
                .AddSingleton<Random>()
                .BuildServiceProvider();
            
            var conf = new DiscordConfiguration()
            {
                LoggerFactory = loggerFactory,
                Token = token,
                Intents = DiscordIntents.All
            };

            client = new DiscordShardedClient(conf);

            var interConf = new InteractivityConfiguration()
            {
                PollBehaviour = PollBehaviour.DeleteEmojis,
                Timeout = TimeSpan.FromMinutes(2),
                ButtonBehavior = ButtonPaginationBehavior.DeleteButtons,
                AckPaginationButtons = true
            };

            await client.UseInteractivityAsync(interConf);

            var slashConf = new SlashCommandsConfiguration()
            {
                Services = services
            };

            var slash = await client.UseSlashCommandsAsync(slashConf);

            foreach (var s in slash.Values)
            {
                s.RegisterCommands<CookingCommands>();
                s.RegisterCommands<Core>(1043194801723551774);

                s.SlashCommandErrored += async (s, e) =>
                {
                    if (e.Exception is SlashExecutionChecksFailedException slex)
                    {
                        foreach (var check in slex.FailedChecks)
                            if (check is Checks.SlashBlockBlacklistAttribute att)
                                await e.Context.CreateResponseAsync("Sorry, but you are blacklisted from using this command!", true);
                    }
                };

                s.ContextMenuErrored += async (s, e) =>
                {
                    if (e.Exception is ContextMenuExecutionChecksFailedException cex)
                    {
                        foreach (var check in cex.FailedChecks)
                            if (check is Checks.ContextRequireOwnerAttribute att)
                                await e.Context.CreateResponseAsync("This context menu command is only allowed for the owner(s) of this app", true);
                    }
                };
            }

            client.Ready += async (sender, args) =>
            {
                var db = await DatabaseManager.GetRecipeNamesAsync();

                var activity = new DiscordActivity(string.Format(StatusFormatString, db.Count));

                await client.UpdateStatusAsync(activity, UserStatus.Online);
            };

            client.Zombied += (sender, args) =>
            {
                ResetFlag = true;
                StopFlag = true;

                return Task.CompletedTask;
            };

            DatabaseManager.OnRecipeCountChanged += UpdateStatusAsync;

            await client.StartAsync();

            while (!StopFlag)
            {
                await Task.Delay(1000);
            }

            await client.StopAsync();

            if (ResetFlag)
            {
                ResetFlag = false;
                StopFlag = false;

                await MainAsync(loggerFactory, token);
            }
            else
            {
                Log.Information("Thank you for using Cooking Bot.");
            }
        }

        const string StatusFormatString = "Cooking {0} recipes!";

        private async Task UpdateStatusAsync()
        {
            var names = await DatabaseManager.GetRecipeNamesAsync();

            var activity = new DiscordActivity(string.Format(StatusFormatString, names.Count));

            await client.UpdateStatusAsync(activity, UserStatus.Online);
        }
    }
}
