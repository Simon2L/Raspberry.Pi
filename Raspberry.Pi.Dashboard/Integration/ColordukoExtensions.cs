namespace Raspberry.Pi.Dashboard.Integration;

public static class ColorDukoExtensions
{
    public static string ToColor(this ColorDukoColors color)
    {
        return color switch
        {
            ColorDukoColors.Red => "#ff0000",
            ColorDukoColors.Blue => "#0000ff",
            ColorDukoColors.Green => "#00ff00",
            ColorDukoColors.Yellow => "#ffff00",
            ColorDukoColors.Purple => "#800080",
            ColorDukoColors.Orange => "#ffA500",
            ColorDukoColors.Cyan => "#00ffff",
            ColorDukoColors.Magenta => "#ff00ff",
            ColorDukoColors.Lime => "#65fe08",
            _ => ""
        };
    }
}

public enum ColorDukoColors
{
    None = 0,
    Red = 1,
    Blue = 2,
    Green = 3,
    Yellow = 4,
    Purple = 5,
    Orange = 6,
    Cyan = 7,
    Magenta = 8,
    Lime = 9
}