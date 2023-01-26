using System.Data;

using MySqlConnector;

using Serilog;

using Newtonsoft.Json;

namespace CookingBot.Data
{
    public delegate Task AsyncEventDelegate();

    public static class DatabaseManager
    {
        private static readonly string connStr = $"Server={AppCredentials.DatabaseAdress};Port={AppCredentials.DatabasePort};Database={AppCredentials.DatabaseName};Uid={AppCredentials.DatabaseUserName};Pwd={AppCredentials.DatabaseUserPassword};";
        private static int ActiveConnections = 0;
        public static event AsyncEventDelegate OnRecipeCountChanged;

        struct DatabaseLogStructures
        {
            public const string ConnectionStructure = "Database connection {Action}. Active connections = {ActiveConnections}";
        }

        private static async Task<MySqlConnection> OpenConnectionAsync()
        {
            while (ActiveConnections >= 30) await Task.Delay(100);
            MySqlConnection conn = new(connStr);
            await conn.OpenAsync();
            ActiveConnections++;
            Log.Information(DatabaseLogStructures.ConnectionStructure, "Opened", ActiveConnections);
            return conn;
        }

        private static async Task CloseConnectionAsync(MySqlConnection conn)
        {
            await conn.CloseAsync();
            ActiveConnections--;
            Log.Information(DatabaseLogStructures.ConnectionStructure, "Closed", ActiveConnections);
        }

        public static async Task<bool> GetConnectionStatus()
        {
            var conn = await OpenConnectionAsync();

            bool result = await conn.PingAsync();

            await CloseConnectionAsync(conn);

            return result;
        }

        public static async Task<List<ulong>> GetBlacklistAsync()
        {
            var conn = await OpenConnectionAsync();
            var cmd = new MySqlCommand("SELECT * FROM blacklist", conn);
            List<ulong> blacklist = new();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                blacklist.Add(reader.GetUInt64(0));

            await reader.CloseAsync();
            await CloseConnectionAsync(conn);
            return blacklist;
        }

        public static async Task<bool> IsBlacklistedAsync(ulong user)
        {
            var conn = await OpenConnectionAsync();
            var cmd = new MySqlCommand("SELECT User FROM blacklist WHERE User = @u", conn);

            cmd.Parameters.AddWithValue("u", user);

            await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleResult);
            bool isBlacklisted = reader.HasRows;

            await reader.CloseAsync();
            await CloseConnectionAsync(conn);
            return isBlacklisted;
        }

        public static async Task AddToBlacklistAsync(ulong user)
        {
            var conn = await OpenConnectionAsync();
            var cmd = new MySqlCommand("INSERT INTO blacklist (User) VALUES (@u)", conn);
            cmd.Parameters.AddWithValue("u", user);
            await cmd.ExecuteNonQueryAsync();
            await CloseConnectionAsync(conn);
        }

        public static async Task RemoveFromBlacklistAsync(ulong user)
        {
            var conn = await OpenConnectionAsync();
            var cmd = new MySqlCommand("DELETE FROM blacklist WHERE User = @u", conn);
            cmd.Parameters.AddWithValue("u", user);
            await cmd.ExecuteNonQueryAsync();
            await CloseConnectionAsync(conn);
        }

        public static async Task<bool> IsManagerAsync(ulong user)
        {
            var conn = await OpenConnectionAsync();
            var cmd = new MySqlCommand("SELECT UserID FROM managers WHERE UserID = @u", conn);

            cmd.Parameters.AddWithValue("u", user);

            await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleResult);
            bool isManager = reader.HasRows;

            await reader.CloseAsync();
            await CloseConnectionAsync(conn);
            return isManager;
        }

        public static async Task AddManagerAsync(ulong user)
        {
            var conn = await OpenConnectionAsync();
            var cmd = new MySqlCommand("INSERT INTO managers (UserID) VALUES (@u)", conn);
            cmd.Parameters.AddWithValue("u", user);
            await cmd.ExecuteNonQueryAsync();
            await CloseConnectionAsync(conn);
        }

        public static async Task RemoveManagerAsync(ulong user)
        {
            var conn = await OpenConnectionAsync();
            var cmd = new MySqlCommand("DELETE FROM managers WHERE UserID = @u", conn);
            cmd.Parameters.AddWithValue("u", user);
            await cmd.ExecuteNonQueryAsync();
            await CloseConnectionAsync(conn);
        }

        public static async Task<List<string>> GetRecipeNamesAsync()
        {
            var conn = await OpenConnectionAsync();
            var cmd = new MySqlCommand("SELECT Name FROM recipes", conn);
            List<string> names = new();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                names.Add(reader.GetString(0));

            await reader.CloseAsync();
            await CloseConnectionAsync(conn);
            return names;
        }

        public static async Task UploadRecipeAsync(Recipe recipe)
        {
            var conn = await OpenConnectionAsync();
            var cmd = new MySqlCommand("INSERT INTO recipes (Name, Uploader, UploaderID, UploadDate, Description, Ingredients, Steps, Tags) VALUES (@name, @uploader, @uploaderID, @uploadDate, @description, @ingredients, @steps, @tags)", conn);

            cmd.Parameters.AddWithValue("name", recipe.Name);
            cmd.Parameters.AddWithValue("uploader", recipe.Uploader);
            cmd.Parameters.AddWithValue("uploaderID", recipe.UploaderID);
            cmd.Parameters.AddWithValue("uploadDate", recipe.UploadDate);
            cmd.Parameters.AddWithValue("description", recipe.Description);
            cmd.Parameters.AddWithValue("ingredients", JsonConvert.SerializeObject(recipe.Ingredients));
            cmd.Parameters.AddWithValue("steps", JsonConvert.SerializeObject(recipe.Steps));
            cmd.Parameters.AddWithValue("tags", (int)recipe.Tags);

            await cmd.ExecuteNonQueryAsync();
            await CloseConnectionAsync(conn);

            await OnRecipeCountChanged.Invoke();
        }

        public static async Task UpdateRecipeUploaderAsync(Recipe recipe)
        {
            var conn = await OpenConnectionAsync();
            var cmd = new MySqlCommand("UPDATE recipes SET Uploader = @uploader WHERE Name = @name", conn);

            cmd.Parameters.AddWithValue("uploader", recipe.Uploader);
            cmd.Parameters.AddWithValue("name", recipe.Name);

            await cmd.ExecuteNonQueryAsync();
            await CloseConnectionAsync(conn);
        }

        public static async Task UpdateRecipeTagsAsync(Recipe recipe)
        {
            var conn = await OpenConnectionAsync();
            var cmd = new MySqlCommand("UPDATE recipes SET Tags = @tags WHERE Name = @name", conn);

            cmd.Parameters.AddWithValue("tags", (int)recipe.Tags);
            cmd.Parameters.AddWithValue("name", recipe.Name);

            await cmd.ExecuteNonQueryAsync();
            await CloseConnectionAsync(conn);
        }

        public static async Task DeleteRecipeAsync(string name)
        {
            var conn = await OpenConnectionAsync();
            var cmd = new MySqlCommand("DELETE FROM recipes WHERE Name = @name", conn);
            cmd.Parameters.AddWithValue("name", name);
            await cmd.ExecuteNonQueryAsync();
            await CloseConnectionAsync(conn);

            await OnRecipeCountChanged.Invoke();
        }

        public static async Task<bool> RecipeExistsAsync(string name)
        {
            var conn = await OpenConnectionAsync();
            var cmd = new MySqlCommand("SELECT Name FROM recipes WHERE Name = @name", conn);

            cmd.Parameters.AddWithValue("name", name);

            await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleResult);
            bool exists = reader.HasRows;

            await reader.CloseAsync();
            await CloseConnectionAsync(conn);
            return exists;
        }

        public static async Task<Recipe?> GetRecipeAsync(string name)
        {
            var conn = await OpenConnectionAsync();
            var cmd = new MySqlCommand("SELECT Name, Uploader, UploaderID, UploadDate, Description, Ingredients, Steps, Tags FROM recipes WHERE Name = @name", conn);
            cmd.Parameters.AddWithValue("name", name);
            Recipe? recipe = null;
            await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow);
            if (reader.HasRows)
            {
                await reader.ReadAsync();
                recipe = new Recipe()
                {
                    Name = reader.GetString("Name"),
                    Uploader = reader.GetString("Uploader"),
                    UploaderID = reader.GetUInt64("UploaderID"),
                    UploadDate = reader.GetDateTimeOffset("UploadDate"),
                    Description = reader.GetString("Description"),
                    Ingredients = JsonConvert.DeserializeObject<List<string>>(reader.GetString("Ingredients")),
                    Steps = JsonConvert.DeserializeObject<List<string>>(reader.GetString("Steps")),
                    Tags = (RecipeTags)reader.GetInt32("Tags")
                };
            }

            await reader.CloseAsync();
            await CloseConnectionAsync(conn);
            return recipe;
        }

        public static async Task<List<string>> GetRecipesByAsync(ulong user)
        {
            var conn = await OpenConnectionAsync();
            var cmd = new MySqlCommand("SELECT Name FROM recipes WHERE UploaderID = @user", conn);
            cmd.Parameters.AddWithValue("user", user);
            List<string> recipes = new();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                recipes.Add(reader.GetString("Name"));
            }

            await reader.CloseAsync();
            await CloseConnectionAsync(conn);
            return recipes;
        }
    }
}
