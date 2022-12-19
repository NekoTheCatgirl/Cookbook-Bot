using DSharpPlus.SlashCommands;

namespace CookingBot.Data
{
    [Flags]
    public enum RecipeTags
    {
        [ChoiceName("None")]
        None = 0,
        [ChoiceName("Vegan")]
        Vegan = 1,
        [ChoiceName("Gluten Free")]
        GlutenFree = 2,
        [ChoiceName("Lactose Free")]
        LactoseFree = 4,
        [ChoiceName("Breakfast")]
        Breakfast = 8,
        [ChoiceName("Lunch")]
        Lunch = 16,
        [ChoiceName("Dinner")]
        Dinner = 32,
        [ChoiceName("Dessert")]
        Dessert = 64,
    }
}
