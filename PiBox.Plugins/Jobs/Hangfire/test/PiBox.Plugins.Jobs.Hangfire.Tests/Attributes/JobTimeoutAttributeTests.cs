using FluentAssertions;
using NUnit.Framework;
using PiBox.Plugins.Jobs.Hangfire.Attributes;

namespace PiBox.Plugins.Jobs.Hangfire.Tests.Attributes
{
    public class JobTimeoutAttributeTests
    {
        public record TestCase(int Value, TimeUnit Unit, TimeSpan Expected);
        private static readonly TestCase[] _testCases =
        [
            new(123, TimeUnit.Milliseconds, TimeSpan.FromMilliseconds(123)),
            new(123, TimeUnit.Seconds, TimeSpan.FromSeconds(123)),
            new(123, TimeUnit.Minutes, TimeSpan.FromMinutes(123)),
            new(123, TimeUnit.Hours, TimeSpan.FromHours(123)),
            new(123, TimeUnit.Days, TimeSpan.FromDays(123)),
        ];

        [Test, TestCaseSource(nameof(_testCases))]
        public void CanInitialize(TestCase testCase)
        {
            var timeoutAttribute = new JobTimeoutAttribute(testCase.Value, testCase.Unit);
            timeoutAttribute.Timeout.Should().Be(testCase.Expected);
        }

        [Test]
        public void ThrowsOnOutOfRangeValue()
        {
            var func = () => new JobTimeoutAttribute(123, (TimeUnit)100);
            func.Invoking(x => x())
                .Should().Throw<ArgumentOutOfRangeException>();
        }
    }
}
