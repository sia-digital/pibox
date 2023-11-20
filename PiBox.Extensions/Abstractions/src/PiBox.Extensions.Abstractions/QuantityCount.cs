namespace PiBox.Extensions.Abstractions
{
    public abstract record QuantityCount(int Value) : Quantity<int>(Value, "Count");
}
