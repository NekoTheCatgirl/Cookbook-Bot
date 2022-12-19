using Serilog;

using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

using CookingBot.Data;
using CookingBot.Checks;

namespace CookingBot.Commands
{
    public class Core : ApplicationCommandModule
    {
        [SlashRequireOwner]
        [SlashCommand("Shutdown", "Turns the bot off")]
        public async Task ShutdownCommand(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync("Shutting down...", true);

            Log.Information("Shutdown command issued by {UserID}", ctx.User.Id);

            Bot.SetStopFlag();
        }

        [ContextRequireOwner]
        [ContextMenu(ApplicationCommandType.UserContextMenu, "Blacklist User")]
        public async Task BlacklistMenu(ContextMenuContext ctx)
        {
            await ctx.DeferAsync(true);
            ulong id = ctx.TargetMember.Id;
            var blacklist = await DatabaseManager.GetBlacklistAsync();

            if (blacklist.Contains(id))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("They are already blacklisted"));
                return;
            }

            await DatabaseManager.AddToBlacklistAsync(id);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Successfully blacklisted the user."));
        }

        [SlashRequireOwner]
        [SlashCommand("Blacklist", "Blacklists a user.")]
        public async Task BlacklistCommand(InteractionContext ctx, [Option("UserID", "The id of the user to blacklist")] string uid)
        {
            await ctx.DeferAsync(true);
            ulong id = ulong.Parse(uid);
            var blacklist = await DatabaseManager.GetBlacklistAsync();

            if (blacklist.Contains(id))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("They are already blacklisted"));
                return;
            }

            await DatabaseManager.AddToBlacklistAsync(id);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Successfully blacklisted the user."));
        }

        [SlashRequireOwner]
        [SlashCommand("Manager", "Sets the manager status for a user.")]
        public async Task ManagerCommand(InteractionContext ctx, [Option("UserID", "The id of the user to set or remove as manager.")] string uid, [Option("State", "The manager state to give to the target.")] bool state)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            ulong id = ulong.Parse(uid);

            var isManager = await DatabaseManager.IsManagerAsync(id);

            if (isManager && !state)
            {
                await DatabaseManager.RemoveManagerAsync(id);
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Removed the manager."));
            }
            else if (!isManager && state)
            {
                await DatabaseManager.AddManagerAsync(id);
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Added them as a manager."));
            }
            else
            {
                if (state)
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("The user is already a manager."));
                }
                else
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("The user is already not a manager."));
                }
            }

        }

        [ContextRequireOwner]
        [ContextMenu(ApplicationCommandType.UserContextMenu, "Whitelist User")]
        public async Task WhitelistMenu(ContextMenuContext ctx)
        {
            await ctx.DeferAsync(true);
            ulong id = ctx.TargetMember.Id;
            var blacklist = await DatabaseManager.GetBlacklistAsync();

            if (!blacklist.Contains(id))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("They are already whitelisted"));
                return;
            }

            await DatabaseManager.RemoveFromBlacklistAsync(id);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Successfully whitelisted the user."));
        }

        [SlashRequireOwner]
        [SlashCommand("Whitelist", "Whitelist a user.")]
        public async Task WhitelistCommand(InteractionContext ctx, [Option("UserID", "The id of the user to whitelist")] string uid)
        {
            await ctx.DeferAsync(true);
            ulong id = ulong.Parse(uid);
            var blacklist = await DatabaseManager.GetBlacklistAsync();

            if (!blacklist.Contains(id))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("They are already whitelisted"));
                return;
            }

            await DatabaseManager.RemoveFromBlacklistAsync(id);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Successfully whitelisted the user."));
        }
    }
}
