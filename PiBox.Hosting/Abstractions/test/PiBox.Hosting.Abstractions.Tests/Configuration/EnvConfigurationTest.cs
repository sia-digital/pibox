using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using PiBox.Hosting.Abstractions.Configuration;

namespace PiBox.Hosting.Abstractions.Tests.Configuration
{
    public class EnvConfigurationTest
    {
        private readonly IDictionary<string, string> _envs = new Dictionary<string, string>();

        private IConfiguration GetConfig()
        {
            foreach (var env in _envs)
                Environment.SetEnvironmentVariable(env.Key, env.Value);

            return new ConfigurationBuilder().AddEnvVariables().Build();
        }

        [SetUp]
        public void Setup()
        {
            // clear before every test run.
            _envs.Clear();
        }

        [Test]
        public void CanGetConfigWithDoubleUnderscores()
        {
            var value = "username";
            _envs["MY__DB__USER"] = value;
            var config = GetConfig();
            var myDbUser = config.GetValue<string>("my:Db:User");
            myDbUser.Should().Be(value);
        }

        [Test]
        public void CanGetConfigWithSingleUnderscores()
        {
            var value = "username";
            _envs["MY_DB_USER"] = value;
            var config = GetConfig();
            var myDbUser = config.GetValue<string>("my:Db:User");
            myDbUser.Should().Be(value);
        }
    }
}
