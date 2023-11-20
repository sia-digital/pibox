using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;

namespace PiBox.Extensions.Abstractions.Tests
{
    public class QuantityTests
    {

        [Test]
        public void CustomQuantityInitConstructorShouldWorkCorrectly()
        {
            var customQuantity = new CustomQuantity { Value = "myValue", Unit = "myUnit" };
            customQuantity.Unit.Should().Be("myUnit");
            customQuantity.Value.Should().Be("myValue");
        }

        [Test]
        public void CountQuantityShouldEnforceUnitCount()
        {
            var loginCount = new LoginQuantityCount(10);
            loginCount.Unit.Should().Be("Count");
            loginCount.Value.Should().Be(10);
        }

        [Test]
        public void CountQuantityShouldSerializeCorrectly()
        {
            var myCounts = new MyCounts() { LoginCount = new LoginQuantityCount(10) };
            myCounts.LoginCount.Value.Should().Be(10);
            var json = JsonSerializer.Serialize(myCounts, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            json.Should().Be("{\"loginCount\":{\"value\":10,\"unit\":\"Count\"}}");
        }

        private class MyCounts
        {
            public LoginQuantityCount LoginCount { get; set; } = null!;
        }

        private record LoginQuantityCount(int Value) : QuantityCount(Value);

        private record CustomQuantity() : Quantity<string>("test", "test");
    }
}
