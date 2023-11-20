namespace PiBox.Extensions.Abstractions
{
    public class CurrencyValue
    {
        public static CurrencyValue Euro(decimal value) => new(value, @"€", @"EUR");
        public static CurrencyValue Dollar(decimal value) => new(value, @"$", @"USD");
        public decimal Value { get; set; }
        public string CurrencySymbol { get; set; }
        public string CurrencyCode { get; set; }

        public CurrencyValue(decimal value, string currencySymbol, string currencyCode)
        {
            Value = value;
            CurrencySymbol = currencySymbol;
            CurrencyCode = currencyCode;
        }
    }
}
