using System.Text;

using Serilog;

using EnumsNET;

using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.EventHandling;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;

using CookingBot.Data;
using CookingBot.Checks;

namespace CookingBot.Commands
{
    public struct LogStructures
    {
        public const string CommandExecutedStructure = "{Command} command executed by ({UserName} : {UserId})";
        public const string CommandErroredStructure = "{Command} command errored:\n{ErrorMessage}";
        public const string CommandTimedOutStructure = "{Command} command timed out.";
        public const string RecipeCreatedStructure = "Recipe {RecipeName} created by ({UserName} : {UserId}) with {IngredientCount} ingredients and {StepCount} steps.\nIngredients:\n{Ingredients}\nSteps:\n{Steps}";
    }

    public class CookingCommands : ApplicationCommandModule
    {
        public Random Random { private get; set; }

        private async Task<List<string>> GetRecipeNamesByTags(List<string> db, RecipeTags tags)
        {
            if (tags == RecipeTags.None)
                return db;
            else
            {
                var recipeNames = new List<string>();

                foreach (var recipe in db)
                {
                    var r = await DatabaseManager.GetRecipeAsync(recipe);

                    if (r.HasValue)
                    {
                        if (r.Value.Tags.HasAnyFlags(tags))
                            recipeNames.Add(recipe);
                    }
                }

                return recipeNames;
            }
        }

        private static IEnumerable<Page> GeneratePagesFromEmbeds(List<DiscordEmbed> embeds)
        {
            int page = 1;
            foreach (var embed in embeds)
            {
                yield return new Page("", new DiscordEmbedBuilder(embed).WithFooter($"Page {page}/{embeds.Count}"));
                page++;
            }
        }

        public static PaginationButtons GeneratePaginationButtons(BaseDiscordClient client)
        {
            return new()
            {
                SkipLeft = new DiscordButtonComponent(ButtonStyle.Secondary, "skip_left", "", false, new DiscordComponentEmoji(DiscordEmoji.FromName(client, ":track_previous:"))),
                Left = new DiscordButtonComponent(ButtonStyle.Secondary, "left", "", false, new DiscordComponentEmoji(DiscordEmoji.FromName(client, ":rewind:"))),
                Stop = new DiscordButtonComponent(ButtonStyle.Secondary, "stop", "", false, new DiscordComponentEmoji(DiscordEmoji.FromName(client, ":stop_button:"))),
                Right = new DiscordButtonComponent(ButtonStyle.Secondary, "right", "", false, new DiscordComponentEmoji(DiscordEmoji.FromName(client, ":fast_forward:"))),
                SkipRight = new DiscordButtonComponent(ButtonStyle.Secondary, "skip_right", "", false, new DiscordComponentEmoji(DiscordEmoji.FromName(client, ":track_next:")))
            };
        }

        [SlashCommand("Info", "Get some neat info about the bot.")]
        public async Task InfoCommand(InteractionContext ctx)
        {
            await ctx.DeferAsync(true);

            Log.Information(LogStructures.CommandExecutedStructure, "Info", ctx.User.Username, ctx.User.Id);

            var owner = await ctx.Client.GetUserAsync(248835673669369856);

            var eb = new DiscordEmbedBuilder()
                .WithTitle("Cooking Bot!")
                .WithDescription("A simple discord bot, that can learn new recipe's and give suggestions for what you could cook today!")
                .WithAuthor(name: owner.Username, iconUrl: owner.AvatarUrl);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(eb.Build()));
        }

        [SlashCommand("Timer", "Starts a timer for you")]
        public async Task TimerCommand(InteractionContext ctx, [Option("For", "What the timer is for")] string reason = "", [Option("Seconds", "The total seconds you want the timer to take")] long seconds = 0, [Option("Minutes", "The total minutes you want the timer to take")] long minutes = 0)
        {
            await ctx.DeferAsync();
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Started the timer"));

            Log.Information(LogStructures.CommandExecutedStructure, "Timer", ctx.User.Username, ctx.User.Id);

            bool defaultTimer = (seconds <= 0 && minutes <= 0);
            var totalTime = defaultTimer ? 5 * 60 * 1000 : seconds * 1000 + (minutes * 60) * 1000;
            await Task.Delay((int)totalTime);
            if (string.IsNullOrEmpty(reason))
            {
                await ctx.Channel.SendMessageAsync("Your timer is done " + ctx.User.Mention);
            }
            else
            {
                await ctx.Channel.SendMessageAsync("Your timer for \"" + reason + "\" is done " + ctx.User.Mention);
            }
            Log.Information("Timer is done");
        }

        [SlashCommandGroup("Convert", "Convert units between metric and imperial")]
        public class ConvertCommandGroup
        {

            [SlashCommand("Temperature", "Convert temperature units between metric and imperial")]
            public async Task ConvertTemperatureCommand(InteractionContext ctx, [Option("Conversion", "The type of conversion you wish to make")] UnitConverterTemperature converter, [Option("Value", "The number you want to convert")] long val)
            {
                await ctx.DeferAsync();

                string response = string.Empty;
                switch (converter)
                {
                    case UnitConverterTemperature.CelsiusToFahrenheit:
                        var ctf = (val * 9f / 5f) + 32f;
                        response = $"{val}°C = {ctf}°F";
                        break;

                    case UnitConverterTemperature.FahrenheitToCelsius:
                        var ftc = (val - 32f) * 5f / 9f;
                        response = $"{val}°F = {ftc}°C";
                        break;
                }
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(response));
                Log.Information(LogStructures.CommandExecutedStructure, "Convert - Temperature", ctx.User.Username, ctx.User.Id);
            }

            [SlashCommand("Mass", "Convert mass units between metric and imperial")]
            public async Task ConvertMassCommand(InteractionContext ctx, [Option("From", "The unit type to convert from")] UnitConverterMass fromType, [Option("To", "The unit type to convert to")] UnitConverterMass toType, [Option("Value", "The number you want to convert")] long val)
            {
                await ctx.DeferAsync();

                string response = string.Empty;

                switch (fromType)
                {
                    case UnitConverterMass.Kilogram:
                        switch (toType)
                        {
                            case UnitConverterMass.Kilogram:
                                response = $"{val}Kg = {val}Kg";
                                break;

                            case UnitConverterMass.Gram:
                                var kgtg = val * 1000;
                                response = $"{val}Kg = {kgtg}g";
                                break;

                            case UnitConverterMass.Milligram:
                                var kgtmg = val * 1000000;
                                response = $"{val}Kg = {kgtmg}mg";
                                break;

                            case UnitConverterMass.Pound:
                                var kgtp = val * 2.205;
                                response = $"{val}Kg ~= {kgtp}pounds";
                                break;

                            case UnitConverterMass.Ounce:
                                var kgto = val * 35.274;
                                response = $"{val}Kg = {kgto}ounce";
                                break;
                        }
                        break;

                    case UnitConverterMass.Gram:
                        switch (toType)
                        {
                            case UnitConverterMass.Kilogram:
                                var gtkg = val / 1000;
                                response = $"{val}g = {gtkg}Kg";
                                break;

                            case UnitConverterMass.Gram:
                                response = $"{val}g = {val}g";
                                break;

                            case UnitConverterMass.Milligram:
                                var gtmg = val * 1000;
                                response = $"{val}g = {gtmg}mg";
                                break;

                            case UnitConverterMass.Pound:
                                var gtp = val / 453.6;
                                response = $"{val}g ~= {gtp}pounds";
                                break;

                            case UnitConverterMass.Ounce:
                                var gto = val / 28.35;
                                response = $"{val}g = {gto}ounce";
                                break;
                        }
                        break;

                    case UnitConverterMass.Milligram:
                        switch (toType)
                        {
                            case UnitConverterMass.Kilogram:
                                var mgtkg = val / 1000000;
                                response = $"{val}mg = {mgtkg}Kg";
                                break;

                            case UnitConverterMass.Gram:
                                var mgtg = val / 1000;
                                response = $"{val}mg = {mgtg}g";
                                break;

                            case UnitConverterMass.Milligram:
                                response = $"{val}mg = {val}mg";
                                break;

                            case UnitConverterMass.Pound:
                                var mgtp = val / 453600;
                                response = $"{val}mg ~= {mgtp}pounds";
                                break;

                            case UnitConverterMass.Ounce:
                                var mgto = val / 28350;
                                response = $"{val}mg = {mgto}ounce";
                                break;
                        }
                        break;

                    case UnitConverterMass.Pound:
                        switch (toType)
                        {
                            case UnitConverterMass.Kilogram:
                                var ptkg = val / 2.205;
                                response = $"{val}pounds ~= {ptkg}Kg";
                                break;

                            case UnitConverterMass.Gram:
                                var ptg = val * 453.6;
                                response = $"{val}pounds ~= {ptg}g";
                                break;

                            case UnitConverterMass.Milligram:
                                var ptmg = val * 453600;
                                response = $"{val}pounds ~= {ptmg}mg";
                                break;

                            case UnitConverterMass.Pound:
                                response = $"{val}pounds = {val}pounds";
                                break;

                            case UnitConverterMass.Ounce:
                                var pto = val * 16;
                                response = $"{val}pounds = {pto}ounce";
                                break;
                        }
                        break;

                    case UnitConverterMass.Ounce:
                        switch (toType)
                        {
                            case UnitConverterMass.Kilogram:
                                var otkg = val / 35.274;
                                response = $"{val}ounce ~= {otkg}Kg";
                                break;

                            case UnitConverterMass.Gram:
                                var otg = val * 28.35;
                                response = $"{val}ounce ~= {otg}g";
                                break;

                            case UnitConverterMass.Milligram:
                                var otmg = val * 28350;
                                response = $"{val}ounce ~= {otmg}mg";
                                break;

                            case UnitConverterMass.Pound:
                                var otp = val / 16;
                                response = $"{val}ounce = {otp}pounds";
                                break;

                            case UnitConverterMass.Ounce:
                                response = $"{val}ounce = {val}ounce";
                                break;
                        }
                        break;
                }

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(response));
                Log.Information(LogStructures.CommandExecutedStructure, "Convert - Mass", ctx.User.Username, ctx.User.Id);
            }

            [SlashCommand("Volume", "Convert volume units between metric and imperial")]
            public async Task ConvertVolumeCommand(InteractionContext ctx, [Option("From", "The unit type to convert from")] UnitConverterVolume fromType, [Option("To", "The unit type to convert to")] UnitConverterVolume toType, [Option("Value", "The number you want to convert")] long val)
            {
                await ctx.DeferAsync();

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("This command is currently work in progress, sorry for the delay."));
                Log.Information(LogStructures.CommandExecutedStructure, "Convert - Volume", ctx.User.Username, ctx.User.Id);
            }
        }

        [SlashCommand("Ping", "Gets the response time of the bot.")]
        public async Task PingCommand(InteractionContext ctx)
        {
            await ctx.DeferAsync(true);

            Log.Information(LogStructures.CommandExecutedStructure, "Ping", ctx.User.Username, ctx.User.Id);
            
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Pong! Response time {ctx.Client.Ping}WS"));
        }

        [SlashCommand("Cookbook", "Gets a cookbook of all the recipes added to this bot!")]
        public async Task CookbookCommand(InteractionContext ctx, [Option("Tags", "The tags you wish to restrict the random result to")] RecipeTags tags = RecipeTags.None)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            Log.Information(LogStructures.CommandExecutedStructure, "Cookbook", ctx.User.Username, ctx.User.Id);

            var db = await GetRecipeNamesByTags(await DatabaseManager.GetRecipeNamesAsync(), tags);

            if (db.Count == 0)
            {
                Log.Warning(LogStructures.CommandErroredStructure, "Cookbook", "Cookbook is empty!");
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Sorry, it would seem the cookbook is empty!"));
            }
            else
            {
                var recipeEmbeds = new List<DiscordEmbed>();

                foreach (var recipe in db)
                {
                    var r = await DatabaseManager.GetRecipeAsync(recipe);
                    if (r.HasValue)
                        recipeEmbeds.Add(await GetRecipeEmbed(ctx, r.Value));
                }

                var pages = GeneratePagesFromEmbeds(recipeEmbeds);

                var message = await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Done getting cookbook, enjoy!"));

                await Task.Delay(1000);

                await message.DeleteAsync();

                await ctx.Channel.SendPaginatedMessageAsync(ctx.User, pages, GeneratePaginationButtons(ctx.Client));
            }
        }

        [SlashCommand("List", "Gets all the recipe names, in a list rather than individual pages")]
        public async Task ListCommand(InteractionContext ctx, [Option("Tags", "The tags you wish to restrict the random result to")] RecipeTags tags = RecipeTags.None)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            Log.Information(LogStructures.CommandExecutedStructure, "List", ctx.User.Username, ctx.User.Id);

            var db = await GetRecipeNamesByTags(await DatabaseManager.GetRecipeNamesAsync(), tags);

            if (db.Count == 0)
            {
                Log.Warning(LogStructures.CommandErroredStructure, "List", "Cookbook is empty!");
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Sorry, it would seem the cookbook is empty!"));
            }
            else
            {
                var sb = new StringBuilder();

                foreach (var recipe in db)
                {
                    var r = await DatabaseManager.GetRecipeAsync(recipe);
                    if (r.HasValue)
                        sb.AppendLine(r.Value.Name);
                }

                var inter = ctx.Client.GetInteractivity();

                var pages = inter.GeneratePagesInEmbed(sb.ToString(), SplitType.Line, new DiscordEmbedBuilder().WithTitle("Recipe list:"));

                var message = await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Done getting the list, enjoy!"));

                await Task.Delay(1000);

                await message.DeleteAsync();

                await ctx.Channel.SendPaginatedMessageAsync(ctx.User, pages, GeneratePaginationButtons(ctx.Client));
            }
        }

        private bool IsInvalidName(string name)
        {
            bool isInvalid = false;
            foreach (var c in name)
            {
                if ((char.IsAscii(c) || char.IsWhiteSpace(c) || c == 'é') is false || (c == '.' || c == '\\' || c == '"'))
                {
                    isInvalid = true;
                }
            }
            return isInvalid;
        }

        public enum ActionEnum
        {
            Add,
            Remove
        }

        [SlashCommand("Modify", "Lets you modify the tags for a recipe.")]
        public async Task ModifyCommand(InteractionContext ctx, [Autocomplete(typeof(FindChoiceProvider))][Option("Recipe", "The name of the recipe")] string name, [Option("Action", "The action to perform.")] ActionEnum action, [Option("Tag", "The tag to add or remove your recipe.")] RecipeTags tag)
        {
            await ctx.DeferAsync(true);

            Log.Information(LogStructures.CommandExecutedStructure, "Modify", ctx.User.Username, ctx.User.Id);

            if (await DatabaseManager.RecipeExistsAsync(name))
            {
                var recipe = await DatabaseManager.GetRecipeAsync(name);

                if (recipe.HasValue)
                {
                    if (recipe.Value.UploaderID == ctx.User.Id)
                    {
                        var r = recipe.Value;
                        if (recipe.Value.Tags.HasFlag(tag) && action == ActionEnum.Remove)
                        {
                            r.Tags &= ~tag;
                        }
                        else if (recipe.Value.Tags.HasFlag(tag) is false && action == ActionEnum.Add)
                        {
                            r.Tags |= tag;
                        }
                        await DatabaseManager.UpdateRecipeTagsAsync(r);
                        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Should now be done."));
                    }
                    else
                    {
                        Log.Warning(LogStructures.CommandErroredStructure, "Modify", "The user executing this command is not the creator of the recipe.");
                        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("You are not the creator of this recipe."));
                    }
                }
                else
                {
                    Log.Fatal(LogStructures.CommandErroredStructure, "Modify", "Database error.");
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Database error."));
                }
            }
            else
            {
                Log.Warning(LogStructures.CommandErroredStructure, "Modify", "Specified recipe does not exist");
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("The target recipe does not exist."));
            }
        }

        [SlashBlockBlacklist]
        [SlashCommand("Create", "Creates a recipe for the bot!")]
        public async Task CreateCommand(InteractionContext ctx, [Option("Name", "The name of your recipe. (50 characters maximum)")] string name, [Option("Ingredients", "The number of ingredients your recipe calls for. Must be greater than 0.")] long numIngredients, [Option("Steps", "The number of steps your recipe requires. Must be greater than 0.")] long numSteps)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            Log.Information(LogStructures.CommandExecutedStructure, "Create", ctx.User.Username, ctx.User.Id);

            #region error handler:
            if (await DatabaseManager.RecipeExistsAsync(name))
            {
                Log.Warning(LogStructures.CommandErroredStructure, "Create", "Recipe name already exists.");
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("That recipe name is taken. Sorry."));
                return;
            }
            if (name.Length > 50)
            {
                Log.Warning(LogStructures.CommandErroredStructure, "Create", "Recipe name was too long.");
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("That recipe name is too long. Sorry."));
                return;
            }
            if (IsInvalidName(name))
            {
                Log.Warning(LogStructures.CommandErroredStructure, "Create", "Recipe name contained illegal characters.");
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("The recipe name contains illegal characters. Aborting."));
                return;
            }
            if (numIngredients <= 0)
            {
                Log.Warning(LogStructures.CommandErroredStructure, "Create", "Ingredient count was less than or equal to 0.");
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("A recipe without any ingredients is called AIR."));
                return;
            }
            if (numSteps <= 0)
            {
                Log.Warning(LogStructures.CommandErroredStructure, "Create", "Step count was less than or equal to 0.");
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("What, is the person making this meant to just throw everything in a bowl and pray? We need some steps!"));
                return;
            }
            #endregion
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Alrighty, lets get started!"));

            var recipe = new Recipe
            {
                Name = name,
                Uploader = ctx.User.Username,
                UploaderID = ctx.User.Id,
                Tags = RecipeTags.None
            };

            await ctx.Channel.SendMessageAsync("Would you be so kind as to shortly describe your recipe? (300 character max)");

            var descResult = await ctx.Channel.GetNextMessageAsync(ctx.User, TimeSpan.FromMinutes(10));

            if (descResult.TimedOut)
            {
                Log.Warning(LogStructures.CommandTimedOutStructure, "Create");
                await ctx.Channel.SendMessageAsync("Sorry, you took too long to respond. Timed out.");
                return;
            }

            if (descResult.Result.Content.Length > 300)
            {
                Log.Warning(LogStructures.CommandErroredStructure, "Create", "Description too long.");
                await ctx.Channel.SendMessageAsync("Sorry, but that description is too long. Aborting action.");
                return;
            }

            recipe.Description = descResult.Result.Content;

            var ingredients = new List<string>();

            for (long i = 1; i <= numIngredients; i++)
            {
                if (i == 1)
                {
                    await ctx.Channel.SendMessageAsync("Lets write down the ingredients now. Whats the name of the first ingredient? (30 character max)");
                }
                else
                {
                    await ctx.Channel.SendMessageAsync($"Okay, and ingredient number {i}, whats its name? (30 character max)");
                }

                var ingNameResult = await ctx.Channel.GetNextMessageAsync(ctx.User);

                if (ingNameResult.TimedOut)
                {
                    Log.Warning(LogStructures.CommandTimedOutStructure, "Create");
                    await ctx.Channel.SendMessageAsync("Sorry, you took too long to respond. Timed out.");
                    return;
                }

                if (ingNameResult.Result.Content.Length > 30)
                {
                    Log.Warning(LogStructures.CommandErroredStructure, "Create", "Ingredient name too long.");
                    await ctx.Channel.SendMessageAsync("Sorry, but that ingredient name is too long. Aborting action.");
                    return;
                }

                await ctx.Channel.SendMessageAsync("Okay, and how much do we need? (30 character max)");

                var ingAmountResult = await ctx.Channel.GetNextMessageAsync(ctx.User);

                if (ingAmountResult.TimedOut)
                {
                    Log.Warning(LogStructures.CommandTimedOutStructure, "Create");
                    await ctx.Channel.SendMessageAsync("Sorry, you took too long to respond. Timed out.");
                    return;
                }

                if (ingAmountResult.Result.Content.Length > 30)
                {
                    Log.Warning(LogStructures.CommandErroredStructure, "Create", "Ingredient amount too long.");
                    await ctx.Channel.SendMessageAsync("Sorry, but that ingredient amount is too long. Aborting action.");
                    return;
                }

                string ingredient = ingNameResult.Result.Content + " - " + ingAmountResult.Result.Content;

                ingredients.Add(ingredient);
            }

            recipe.Ingredients = ingredients;

            await ctx.Channel.SendMessageAsync("Alright, lets get those steps down.");

            var steps = new List<string>();

            for (long s = 1; s <= numSteps; s++)
            {
                await ctx.Channel.SendMessageAsync($"Okay, describe step {s}. (200 character max)");

                var stepResult = await ctx.Channel.GetNextMessageAsync(ctx.User, TimeSpan.FromMinutes(2));

                if (stepResult.TimedOut)
                {
                    Log.Warning(LogStructures.CommandTimedOutStructure, "Create");
                    await ctx.Channel.SendMessageAsync("Sorry, you took too long to respond. Timed out.");
                    return;
                }

                if (stepResult.Result.Content.Length > 200)
                {
                    Log.Warning(LogStructures.CommandErroredStructure, "Create", "Step description too long.");
                    await ctx.Channel.SendMessageAsync("Sorry, but this step is too long. Aborting action.");
                    return;
                }

                steps.Add(stepResult.Result.Content);
            }

            recipe.Steps = steps;

            var statusMessage = await ctx.Channel.SendMessageAsync("Okay, that should be all. Creating the entry in the database now. Please wait...");

            recipe.UploadDate = DateTimeOffset.UtcNow;

            if (await DatabaseManager.RecipeExistsAsync(name))
            {
                Log.Warning(LogStructures.CommandErroredStructure, "Create", "Recipe was recently created by another user.");
                await ctx.Channel.SendMessageAsync("Sorry, it would seem someone just created a recipe with this name before you. Avoiding overwrite.");
                return;
            }

            await DatabaseManager.UploadRecipeAsync(recipe);

            await NotifyNewRecipeEvent(ctx, recipe);

            await statusMessage.ModifyAsync("Done, here is your recipe:", recipe.BuildEmbed());
        }

        private async Task NotifyNewRecipeEvent(InteractionContext ctx, Recipe recipe)
        {
            var guild = await ctx.Client.GetGuildAsync(1043194801723551774);
            var channel = guild.GetChannel(1043272347907522630);
            await channel.SendMessageAsync($"New recipe created, by user {recipe.Uploader}. Their id is {recipe.UploaderID}", recipe.BuildEmbed());

            Log.Information(LogStructures.RecipeCreatedStructure, recipe.Name, recipe.Uploader, recipe.UploaderID, recipe.Ingredients.Count, recipe.Steps.Count, recipe.GenerateIngredients(), recipe.GenerateSteps());
        }

        [RequireManagerOrOwner]
        [SlashCommand("Remove", "Removes a recipe, only accessible to the owner to remove recipes that break the rules.")]
        public async Task RemoveCommand(InteractionContext ctx, [Autocomplete(typeof(FindChoiceProvider))][Option("Name", "The name of the recipe to remove (case sensetive)")] string name)
        {
            await ctx.DeferAsync(true);

            Log.Information(LogStructures.CommandExecutedStructure, "Remove", ctx.User.Username, ctx.User.Id);

            if (await DatabaseManager.RecipeExistsAsync(name))
            {
                await DatabaseManager.DeleteRecipeAsync(name);

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Deleted the recipe."));
            }
            else
            {
                Log.Warning(LogStructures.CommandErroredStructure, "Remove", "Specified recipe does not exist");
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("The specified recipe does not exist."));
            }
        }

        public class FindChoiceProvider : IAutocompleteProvider
        {
            public async Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext ctx)
            {
                var db = await DatabaseManager.GetRecipeNamesAsync();

                var choices = new List<DiscordAutoCompleteChoice>();

                foreach (var s in db)
                {
                    if (s.Contains((string)ctx.FocusedOption.Value))
                        choices.Add(new DiscordAutoCompleteChoice(s, s));
                }

                return choices;
            }
        }

        [SlashCommand("Find", "Attempts to find a specific recipe.")]
        public async Task FindCommand(InteractionContext ctx, [Autocomplete(typeof(FindChoiceProvider)), Option("Name", "The name of the recipe you want to find.")] string name)
        {
            await ctx.DeferAsync();

            Log.Information(LogStructures.CommandExecutedStructure, "Find", ctx.User.Username, ctx.User.Id);

            if (await DatabaseManager.RecipeExistsAsync(name))
            {
                var recipe = await DatabaseManager.GetRecipeAsync(name);

                if (recipe.HasValue)
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Here is your recipe:").AddEmbed(await GetRecipeEmbed(ctx, recipe.Value)));
                }
                else
                {
                    Log.Warning(LogStructures.CommandErroredStructure, "Find", "Failed to find recipe");
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("There was an error while trying to find the recipe..."));
                }
            }
            else
            {
                Log.Warning(LogStructures.CommandErroredStructure, "Find", "Recipe does not exist");
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("The specified recipe does not exist."));
            }
        }

        private async Task<DiscordEmbed> GetRecipeEmbed(InteractionContext ctx, Recipe recipe)
        {
            var user = await ctx.Client.GetUserAsync(recipe.UploaderID);

            if (user != null)
            {
                return await recipe.BuildEmbedAsync(user.Username);
            }

            return recipe.BuildEmbed();
        }

        private class RandomValidRecipeResult
        {
            public RandomValidRecipeResult(bool ranOutOfAttempts)
            {
                RanOutOfAttempts = ranOutOfAttempts;
            }

            public RandomValidRecipeResult(bool ranOutOfAttempts, Recipe result)
            {
                RanOutOfAttempts = ranOutOfAttempts;
                Result = result;
            }

            public bool RanOutOfAttempts { get; }
            public Recipe Result { get; }
        }

        private async Task<RandomValidRecipeResult> GetRandomValidRecipe(List<string> db, int attempts)
        {
            for (int attempt = 0; attempt < attempts; attempt++)
            {
                int rindex = Random.Next(0, db.Count);
                var recipe = await DatabaseManager.GetRecipeAsync(db[rindex]);
                if (recipe.HasValue)
                {
                    return new RandomValidRecipeResult(false, recipe.Value);
                }
            }
            return new RandomValidRecipeResult(true);
        }

        [SlashCommand("Random", "Get a random recipe from the entire cookbook.")]
        public async Task RandomCommand(InteractionContext ctx, [Option("Tags", "The tags you wish to restrict the random result to")] RecipeTags tags = RecipeTags.None)
        {
            await ctx.DeferAsync();

            Log.Information(LogStructures.CommandExecutedStructure, "Random", ctx.User.Username, ctx.User.Id);

            var db = await GetRecipeNamesByTags(await DatabaseManager.GetRecipeNamesAsync(), tags);

            if (db.Count > 1)
            {
                var validResult = await GetRandomValidRecipe(db, 5);

                if (validResult.RanOutOfAttempts)
                {
                    Log.Warning(LogStructures.CommandErroredStructure, "Random", "Ran out of attempts.");
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("There were multiple errors while getting a random recipe, something must be wrong with the database. Im sorry for this inconvinience, try the command again or contact the owner."));
                }
                else
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Here is a random recipe!").AddEmbed(await GetRecipeEmbed(ctx, validResult.Result)));
                }
            }
            else
            {
                Log.Warning(LogStructures.CommandErroredStructure, "Random", "Not enough recipes.");
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Sorry, there is not enough recipes to give a random one."));
            }
        }
    }
}
