namespace Raspberry.Pi.Dashboard.Domain;

public record RGB(int R, int G, int B)
{
    public bool EqualsScuffed(RGB other)
    {
        return (R == other.R && G == other.G && B == other.B);
    }
    public int ToInt()
    {
        return (R << 16) | (G << 8) | B;
    }
};
