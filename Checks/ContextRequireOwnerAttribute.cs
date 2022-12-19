using DSharpPlus.SlashCommands;

namespace CookingBot.Checks
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ContextRequireOwnerAttribute : ContextMenuCheckBaseAttribute
    {
        public override Task<bool> ExecuteChecksAsync(ContextMenuContext ctx)
        {
            var app = ctx.Client.CurrentApplication;
            var me = ctx.Client.CurrentUser;

            return app != null ? Task.FromResult(app.Owners.Any(x => x.Id == ctx.User.Id)) : Task.FromResult(ctx.User.Id == me.Id);
        }
    }
}
