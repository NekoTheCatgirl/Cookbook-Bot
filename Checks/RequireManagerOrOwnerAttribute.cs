using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

using CookingBot.Data;

namespace CookingBot.Checks
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class RequireManagerOrOwnerAttribute : SlashCheckBaseAttribute
    {
        public override async Task<bool> ExecuteChecksAsync(InteractionContext ctx)
        {
            var app = ctx.Client.CurrentApplication;
            var me = ctx.Client.CurrentUser;

            var isOwner = app != null ? app.Owners.Any(x => x.Id == ctx.User.Id) : ctx.User.Id == me.Id;
            var isManager = await DatabaseManager.IsManagerAsync(ctx.User.Id);

            return isOwner || isManager;
        }
    }
}
