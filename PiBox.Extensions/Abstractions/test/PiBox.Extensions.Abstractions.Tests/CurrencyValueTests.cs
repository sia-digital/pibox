using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;

namespace PiBox.Extensions.Abstractions.Tests
{
    public class CurrencyValueTests
    {
        [Test]
        public void CurrencyValueEuroWorksAsExpected()
        {
            var euro = CurrencyValue.Euro(new decimal(100.99));
            euro.Value.Should().Be((decimal)100.99);
            euro.CurrencySymbol.Should().Be("€");
            euro.CurrencyCode.Should().Be("EUR");
            var jsonSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            var json = JsonSerializer.Serialize(euro, jsonSerializerOptions);
            json.Should().Be("{\"value\":100.99,\"currencySymbol\":\"\\u20AC\",\"currencyCode\":\"EUR\"}");
            var deserialize = JsonSerializer.Deserialize<CurrencyValue>(json, jsonSerializerOptions);
            deserialize!.Value.Should().Be((decimal)100.99);
            deserialize.CurrencySymbol.Should().Be("€");
            deserialize.CurrencyCode.Should().Be("EUR");
        }

        [Test]
        public void CurrencyValueDollarWorksAsExpected()
        {
            var euro = CurrencyValue.Dollar(new decimal(10.99));
            euro.Value.Should().Be((decimal)10.99);
            euro.CurrencySymbol.Should().Be("$");
            euro.CurrencyCode.Should().Be("USD");
            var jsonSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            var json = JsonSerializer.Serialize(euro, jsonSerializerOptions);
            json.Should().Be("{\"value\":10.99,\"currencySymbol\":\"$\",\"currencyCode\":\"USD\"}");
            var deserialize = JsonSerializer.Deserialize<CurrencyValue>(json, jsonSerializerOptions);
            deserialize!.Value.Should().Be((decimal)10.99);
            deserialize.CurrencySymbol.Should().Be("$");
            deserialize.CurrencyCode.Should().Be("USD");
        }

        [Test]
        public void CurrencyValueShouldSerializeCorrectly()
        {
            var euro = new CurrencyValue(new decimal(999.99), "£", "GBP");
            var jsonSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            var json = JsonSerializer.Serialize(euro, jsonSerializerOptions);
            json.Should().Be("{\"value\":999.99,\"currencySymbol\":\"\\u00A3\",\"currencyCode\":\"GBP\"}");
            var deserialize = JsonSerializer.Deserialize<CurrencyValue>(json, jsonSerializerOptions);
            deserialize!.Value.Should().Be((decimal)999.99);
            deserialize.CurrencySymbol.Should().Be("£");
            deserialize.CurrencyCode.Should().Be("GBP");
        }
    }
}
