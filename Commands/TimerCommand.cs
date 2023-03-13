using Serilog;

using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.EventHandling;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using System.Xml.XPath;
using System;
using System.ComponentModel.DataAnnotations;

namespace CookingBot.Commands
{
    internal class TimerCommandModule : ApplicationCommandModule
    {
        private static Dictionary<string, UserTimer> Timers = new();

        struct TimerButtonID
        {
            public const string CANCEL_BUTTON_ID = "cancel_button";
            public const string ADD_TIME_BUTTON_ID = "add_time_button";
        }

        class UserTimer
        {
            private long totalTime;
            public long TotalTime 
            {
                get => totalTime;
                set
                {
                    if (value > 24 * 60 * 60)
                    {
                        totalTime = 24 * 60 * 60;
                    }
                    else
                    {
                        totalTime = value;
                    }
                }
            }
            public ulong UserID { get; set; }
            public DiscordEmbedBuilder Embed { get; set; }
            public InteractionContext Context { get; set; }
            public System.Timers.Timer timer { get; set; }

            public void AddTime(long time)
            {
                TotalTime += time;
            }
        }

        static DiscordButtonComponent cancelButtonDisabled = new DiscordButtonComponent(ButtonStyle.Danger, TimerButtonID.CANCEL_BUTTON_ID, "Cancel", true);
        static DiscordButtonComponent addTimeButtonDisabled = new DiscordButtonComponent(ButtonStyle.Secondary, TimerButtonID.ADD_TIME_BUTTON_ID, "Add 5 minutes", true);

        public static async Task TimerComponentInteractionCreated(object sender, ComponentInteractionCreateEventArgs e)
        {
            await e.Interaction.DeferAsync(true);
            if (e.Id.StartsWith(TimerButtonID.CANCEL_BUTTON_ID) || e.Id.StartsWith(TimerButtonID.ADD_TIME_BUTTON_ID))
            {
                var idComponents = e.Id.Split('.');
                var guid = idComponents[1];
                if (Timers.ContainsKey(guid))
                {
                    if (e.User.Id == Timers[guid].UserID)
                    {
                        if (e.Id.StartsWith(TimerButtonID.CANCEL_BUTTON_ID))
                        {
                            Timers[guid].timer.Stop();

                            Timers[guid].Embed.WithDescription("Timer canceled.");

                            await Timers[guid].Context.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(Timers[guid].Embed).AddComponents(cancelButtonDisabled, addTimeButtonDisabled));

                            await e.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent("Canceled the timer."));
                        }
                        else if (e.Id.StartsWith(TimerButtonID.ADD_TIME_BUTTON_ID))
                        {
                            if (Timers[guid].TotalTime == 24 * 60 * 60)
                            {
                                await e.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent("Sorry, but the timers total time exceeds 24 hours, so we cant add any more time."));
                            }
                            else
                            {
                                Timers[guid].AddTime(5 * 60);

                                await e.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent("Added 5 minutes."));
                            }
                        }
                    }
                    else
                    {
                        await e.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent("Sorry, but only the one who created this timer can interact with it."));
                    }
                }
                else
                {
                    await e.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent("An error seems to have occured. Sorry for the inconvinience"));
                }
            }
        }

        [SlashCommand("Timer", "Starts a timer for you, only whole numbers work, limit 24 hours")]
        public async Task TimerCommand(InteractionContext ctx, [Option("For", "What the timer is for")] string reason = "", [Option("Seconds", "The total seconds you want the timer to take, only whole numbers work")] long seconds = 0, [Option("Minutes", "The total minutes you want the timer to take, only whole numbers work")] long minutes = 0, [Option("Hour", "The total hours you want the timer to take, only whole numbers work")] long hours = 0)
        {
            await ctx.DeferAsync();

            Log.Information(LogStructures.CommandExecutedStructure, "Timer", ctx.User.Username, ctx.User.Id);

            if (seconds < 0) seconds = 0;

            if (minutes < 0) minutes = 0;

            if (hours < 0) hours = 0;

            if (seconds <= 0 && minutes <= 0 && hours <= 0)
            {
                minutes = 5;
            }

            string title = reason.Length > 0 ? $"Your timer for \"{reason}\" has been started" : $"Your timer has been started";

            var userTimer = new UserTimer()
            {
                TotalTime = (seconds + (minutes * 60) + (hours * 60 * 60)),
                Embed = new DiscordEmbedBuilder()
                    .WithTitle(title)
                    .WithDescription($"Time remaining: {(hours > 0
                            ? $"{hours:00}:{minutes:00}:{seconds:00}"
                            : $"{minutes:00}:{seconds:00}")}")
                    .WithAuthor(name: ctx.User.Username, iconUrl: ctx.User.AvatarUrl),
                UserID = ctx.User.Id,
                Context = ctx,
                timer = new() { Interval = 1000 }
            };

            var startTime = DateTime.Now;

            var guid = Guid.NewGuid().ToString();

            var cancelButton = new DiscordButtonComponent(ButtonStyle.Danger, TimerButtonID.CANCEL_BUTTON_ID + "." + guid, "Cancel", false);
            var addTimeButton = new DiscordButtonComponent(ButtonStyle.Secondary, TimerButtonID.ADD_TIME_BUTTON_ID + "." + guid, "Add 5 minutes", false);

            var message = await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(userTimer.Embed.Build()).AddComponents(cancelButton, addTimeButton));

            int secondsElapsed = 0;

            userTimer.timer.Interval = 1000;
            Timers.Add(guid, userTimer);

            Timers[guid].timer.Elapsed += async (sender, e) =>
            {
                TimeSpan elapsedTime = DateTime.Now - startTime;
                long remainingSeconds = Timers[guid].TotalTime - (long)elapsedTime.TotalSeconds;
                if (remainingSeconds > 0)
                {
                    secondsElapsed++;
                    if (secondsElapsed >= 1)
                    {
                        secondsElapsed = 0;

                        long hoursRemaining = remainingSeconds / 3600;
                        long minutesRemaining = (remainingSeconds % 3600) / 60;
                        long secondsRemaining = remainingSeconds % 60;

                        string remainingTimeString = hoursRemaining > 0
                            ? $"{hoursRemaining:00}:{minutesRemaining:00}:{secondsRemaining:00}"
                            : $"{minutesRemaining:00}:{secondsRemaining:00}";

                        Timers[guid].Embed.WithDescription($"Time remaining: {remainingTimeString}");

                        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(Timers[guid].Embed.Build()).AddComponents(cancelButton, addTimeButton));
                    }
                }
                else
                {
                    Timers[guid].timer.Stop();

                    Timers[guid].Embed.WithDescription($"Timer is done.");

                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(Timers[guid].Embed.Build()).AddComponents(cancelButtonDisabled, addTimeButtonDisabled));

                    if (string.IsNullOrEmpty(reason))
                    {
                        await ctx.Channel.SendMessageAsync("Your timer is done " + ctx.User.Mention);
                    }
                    else
                    {
                        await ctx.Channel.SendMessageAsync("Your timer for \"" + reason + "\" is done " + ctx.User.Mention);
                    }
                }
            };

            Timers[guid].timer.Start();
        }
    }
}
