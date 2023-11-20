namespace PiBox.Extensions.Abstractions
{
    public abstract record Quantity<T>(T Value, string Unit);
}
