using DSharpPlus.SlashCommands;

namespace CookingBot.Data
{
    public enum UnitConverterTemperature
    {
        [ChoiceName("Celsius to Fahrenheit")]
        CelsiusToFahrenheit,
        [ChoiceName("Fahrenheit to Celsius")]
        FahrenheitToCelsius,
    }

    public enum UnitConverterMass
    {
        [ChoiceName("Metric - Kilogram")]
        Kilogram,
        [ChoiceName("Metric - Gram")]
        Gram,
        [ChoiceName("Metric - Milligram")]
        Milligram,
        [ChoiceName("Imperial - Pound")]
        Pound,
        [ChoiceName("Imperial - Ounce")]
        Ounce,
    }

    public enum UnitConverterVolume
    {
        [ChoiceName("Metric - Liter")]
        Liter,
        [ChoiceName("Metric - Milliliter")]
        Milliliter,

    }
}
