using System.Text;

using DSharpPlus.Entities;

namespace CookingBot.Data
{
    public struct Recipe
    {
        public string Name { get; set; }
        public string Uploader { get; set; }
        public ulong UploaderID { get; set; }
        public DateTimeOffset UploadDate { get; set; }
        public string Description { get; set; }
        public List<string> Ingredients { get; set; }
        public List<string> Steps { get; set; }
        public RecipeTags Tags { get; set; }

        public DiscordEmbed BuildEmbed()
        {
            var eb = new DiscordEmbedBuilder();
            eb.WithTitle(Name);

            eb.WithDescription(Description);

            eb.AddField("Ingredients:", GenerateIngredients());

            eb.AddField("Steps:", GenerateSteps());

            if (Tags != RecipeTags.None)
                eb.AddField("Tags:", GenerateTags());

            eb.WithColor(DiscordColor.Azure);
            eb.WithTimestamp(UploadDate);
            eb.WithAuthor($"Created by: {Uploader}");
            return eb.Build();
        }

        public async Task<DiscordEmbed> BuildEmbedAsync(string CurrentUserName)
        {
            if (!string.IsNullOrEmpty(CurrentUserName) && CurrentUserName != Uploader)
            {
                Uploader = CurrentUserName;

                await DatabaseManager.UpdateRecipeUploaderAsync(this);
            }

            return BuildEmbed();
        }

        public bool HasTags()
        {
            return Tags == RecipeTags.None;
        }

        public string GenerateTags()
        {
            var sb = new StringBuilder();

            foreach (var val in Enum.GetValues<RecipeTags>())
            {
                if (Tags.HasFlag(val) && val != RecipeTags.None)
                {
                    if (sb.Length > 0) sb.Append(", ");
                    sb.Append(val.ToString());
                }
            }

            return sb.ToString();
        }

        public string GenerateIngredients()
        {
            var sb = new StringBuilder();

            foreach (var ingredient in Ingredients)
            {
                sb.AppendLine(ingredient);
            }

            return sb.ToString();
        }

        public string GenerateSteps()
        {
            var sb = new StringBuilder();

            int st = 1;
            foreach (var step in Steps)
            {
                sb.AppendLine($"{st}. " + step);
                st++;
            }

            return sb.ToString();
        }
    }
}
