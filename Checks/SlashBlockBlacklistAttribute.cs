using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

using CookingBot.Data;

namespace CookingBot.Checks
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class SlashBlockBlacklistAttribute : SlashCheckBaseAttribute 
    {
        public override async Task<bool> ExecuteChecksAsync(InteractionContext ctx)
        {
            if (await DatabaseManager.IsBlacklistedAsync(ctx.User.Id))
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
