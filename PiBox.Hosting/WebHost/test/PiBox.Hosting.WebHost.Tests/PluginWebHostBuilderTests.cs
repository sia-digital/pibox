using System.Globalization;
using AspNetCoreRateLimit;
using Chronos.Abstractions;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetEscapades.Configuration.Yaml;
using NUnit.Framework;
using PiBox.Hosting.Abstractions.Configuration;
using PiBox.Hosting.Abstractions.DependencyInjection;
using PiBox.Hosting.Abstractions.Extensions;
using PiBox.Hosting.Abstractions.Middlewares.Models;
using PiBox.Hosting.WebHost.Configurators;
using PiBox.Hosting.WebHost.Logging;
using PiBox.Testing;
using Serilog.Core;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace PiBox.Hosting.WebHost.Tests
{
    public class PluginWebHostBuilderTests
    {
        [Test]
        public async Task ShouldWork()
        {
            var pluginWebHostBuilder = PluginWebHostBuilder.Create(new Type[] { typeof(TypeImplementationResolverTests.UnitTestPluginConfig) });
            var host = pluginWebHostBuilder.Build();

            var cultureInfo = CultureInfo.InvariantCulture;
            CultureInfo.CurrentCulture.Should().Be(cultureInfo);
            CultureInfo.CurrentUICulture.Should().Be(cultureInfo);
            CultureInfo.DefaultThreadCurrentCulture.Should().Be(cultureInfo);
            CultureInfo.DefaultThreadCurrentUICulture.Should().Be(cultureInfo);

            var webApplication = host.As<WebApplication>();
            webApplication.Logger.GetType().Name.Should().Be("SerilogLogger");
            var configurationProviders = webApplication.Configuration.As<IConfigurationRoot>().Providers.ToList();

            configurationProviders[0].Should().BeOfType<YamlConfigurationProvider>();
            configurationProviders[0].As<YamlConfigurationProvider>().Source.Path.Should().Be("appsettings.yml");

            configurationProviders[1].Should().BeOfType<YamlConfigurationProvider>();
            configurationProviders[1].As<YamlConfigurationProvider>().Source.Path.Should().Be("appsettings.test.yml");

            configurationProviders[2].Should().BeOfType<JsonConfigurationProvider>();
            configurationProviders[2].As<JsonConfigurationProvider>().Source.Path.Should().Be("appsettings.test.secrets.json");

            configurationProviders[3].Should().BeOfType<EnvConfigurationProvider>();

            var logLevel = webApplication.Configuration.GetValue<string>("serilog:minimumLevel");
            logLevel.Should().Be("Information"); // overriden from appsettings.test.secrets.json

            var ipRateLimitOptions = webApplication.Configuration.BindToSection<IpRateLimitOptions>("IpRateLimiting");
            ipRateLimitOptions.EnableEndpointRateLimiting.Should().BeFalse();
            ipRateLimitOptions.StackBlockedRequests.Should().BeFalse();
            ipRateLimitOptions.RealIpHeader.Should().Be("X-Real-IP");
            ipRateLimitOptions.ClientIdHeader.Should().Be("X-ClientId");
            ipRateLimitOptions.HttpStatusCode.Should().Be(429);
            ipRateLimitOptions.IpWhitelist[0].Should().Be("127.0.0.1");
            ipRateLimitOptions.IpWhitelist[1].Should().Be("::1/10");
            ipRateLimitOptions.IpWhitelist[2].Should().Be("192.168.0.0/24");
            ipRateLimitOptions.EndpointWhitelist[0].Should().Be("get:/api/license");
            ipRateLimitOptions.EndpointWhitelist[1].Should().Be("*:/api/status");
            ipRateLimitOptions.ClientWhitelist[0].Should().Be("dev-id-1");
            ipRateLimitOptions.ClientWhitelist[1].Should().Be("dev-id-2");
            ipRateLimitOptions.GeneralRules[0].Endpoint.Should().Be("*");
            ipRateLimitOptions.GeneralRules[0].Limit.Should().Be(2.0);
            ipRateLimitOptions.GeneralRules[0].Period.Should().Be("1s");
            ipRateLimitOptions.GeneralRules[1].Endpoint.Should().Be("*");
            ipRateLimitOptions.GeneralRules[1].Limit.Should().Be(100.0);
            ipRateLimitOptions.GeneralRules[1].Period.Should().Be("15m");
            ipRateLimitOptions.GeneralRules[2].Endpoint.Should().Be("*");
            ipRateLimitOptions.GeneralRules[2].Limit.Should().Be(1000.0);
            ipRateLimitOptions.GeneralRules[2].Period.Should().Be("12h");
            ipRateLimitOptions.GeneralRules[3].Endpoint.Should().Be("*");
            ipRateLimitOptions.GeneralRules[3].Limit.Should().Be(10000.0);
            ipRateLimitOptions.GeneralRules[3].Period.Should().Be("7d");

            var ipRateLimitPolicies = webApplication.Configuration.BindToSection<IpRateLimitPolicies>("IpRateLimitPolicies");
            ipRateLimitPolicies.IpRules.Should().NotBeNullOrEmpty();
            ipRateLimitPolicies.IpRules[0].Ip.Should().Be("84.247.85.224");
            ipRateLimitPolicies.IpRules[0].Rules[0].Endpoint.Should().Be("*");
            ipRateLimitPolicies.IpRules[0].Rules[0].Limit.Should().Be(10.0);
            ipRateLimitPolicies.IpRules[0].Rules[0].Period.Should().Be("1s");

            var endpointDataSources = webApplication.As<IEndpointRouteBuilder>().DataSources.ToList();
            endpointDataSources.Should().HaveCount(2);
            endpointDataSources[0].Endpoints[0].As<RouteEndpoint>().RoutePattern.RawText.Should().Be("/health/liveness");
            endpointDataSources[0].Endpoints[1].As<RouteEndpoint>().RoutePattern.RawText.Should().Be("/health/readiness");
            endpointDataSources[1].GetType().Name.Should().Be("ControllerActionEndpointDataSource");

            var dateTimeProviderFactory = host.Services.GetRequiredService<IFactory<IDateTimeProvider>>();
            dateTimeProviderFactory.Should().NotBeNull();
            var dateTimeProvider = dateTimeProviderFactory.Create();
            dateTimeProvider.Should().NotBeNull();

            var logger = host.Services.GetRequiredService<ILogger>();
            logger.Should().BeOfType<Logger<ILogger>>();
            var logEventSink = host.Services.GetService<ILogEventSink>();
            logEventSink.Should().BeOfType<LoggingMetricSink>();

            var corsPolicy = webApplication.Configuration.BindToSection<CorsPolicy>(PiBoxWebHostDefaults.CorsConfigSectionName);
            AssertCorsPolicy(corsPolicy, false);

            var corsPolicyProvider = host.Services.GetRequiredService<ICorsPolicyProvider>();
            var policy = await corsPolicyProvider.GetPolicyAsync(new DefaultHttpContext(), PiBoxWebHostDefaults.CorsPolicyName);
            AssertCorsPolicy(policy!, true);

            var apiBehaviourOptions = host.Services.GetRequiredService<IOptions<ApiBehaviorOptions>>().Value;
            apiBehaviourOptions.Should().NotBeNull();
            apiBehaviourOptions.InvalidModelStateResponseFactory.Should().NotBeNull();
            var modelStateResponseFactory = ServiceConfigurator.WriteValidationErrorResponse;
            apiBehaviourOptions.InvalidModelStateResponseFactory.Should().Be(modelStateResponseFactory);

            var sampleConfig = webApplication.Configuration.BindToSection<TypeImplementationResolverTests.UnitTestPluginConfig>("sampleConfig");
            sampleConfig.Name.Should().Be("example1"); // overridden from appsettings.test.yml
            var unitTestPluginConfig = host.Services.GetRequiredService<TypeImplementationResolverTests.UnitTestPluginConfig>();
            unitTestPluginConfig.Should().NotBeNull();

            var hostedServices = host.Services.GetServices<IHostedService>();
            hostedServices.Should().Satisfy(
                service => service.GetType().Name == "TelemetryHostedService",
                service => service.GetType().Name == "GenericWebHostService",
                service => service.GetType().Name == "HealthCheckPublisherHostedService"
            );

            var healthchecks = host.Services.GetServices<IOptions<HealthCheckServiceOptions>>();
            healthchecks.FirstOrDefault()!.Value.Registrations.Should().Satisfy(
                x => x.Name == "api" && x.Tags.Contains("liveness") && x.FailureStatus == HealthStatus.Unhealthy,
                x => x.Name == "disk_space" && x.Tags.Contains("liveness") && x.FailureStatus == HealthStatus.Unhealthy,
                x => x.Name == "memory" && x.Tags.Contains("liveness") && x.FailureStatus == HealthStatus.Unhealthy
            );
        }

        [Test]
        public void ModelStateErrorsGetsMappedIntoValidationErrorResponse()
        {
            var modelState = new ModelStateDictionary();
            modelState.AddModelError("test", "test is invalid");
            modelState.SetModelValue("test2", new ValueProviderResult());
            modelState.SetModelValue("test3", new ValueProviderResult()); ;
            modelState.MarkFieldValid("test3");
            modelState.TryAddModelException("test4", new Vogen.ValueObjectValidationException("vogen is invalid"));
            var httpContext = new DefaultHttpContext { TraceIdentifier = "request-1", RequestServices = TestingDefaults.ServiceCollection().BuildServiceProvider() };
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor(), modelState);
            var mappedResult = ServiceConfigurator.WriteValidationErrorResponse(actionContext);
            mappedResult.Should().NotBeNull();
            mappedResult.Should().BeOfType<BadRequestObjectResult>();
            var badRequestObjectResult = mappedResult as BadRequestObjectResult;
            var resultObject = badRequestObjectResult!.Value;
            resultObject.Should().NotBeNull();
            resultObject.Should().BeOfType<ValidationErrorResponse>();
            var validationErrorResponse = resultObject as ValidationErrorResponse;
            validationErrorResponse.Should().NotBeNull();
            validationErrorResponse!.RequestId.Should().Be("request-1");
            validationErrorResponse.ValidationErrors.Should().HaveCount(2);
            validationErrorResponse.Message.Should().Be("One or more validations have failed.");
            var errors = validationErrorResponse.ValidationErrors.ToArray();
            var fieldValidationError = errors[0];
            fieldValidationError.Field.Should().Be("test");
            fieldValidationError.ValidationMessage.Should().Be("test is invalid");
            fieldValidationError = errors[1];
            fieldValidationError.Field.Should().Be("test4");
            fieldValidationError.ValidationMessage.Should().Be("vogen is invalid");
        }

        private static void AssertCorsPolicy(CorsPolicy corsPolicy, bool allowHeader)
        {
            corsPolicy.AllowAnyOrigin.Should().BeFalse();
            corsPolicy.AllowAnyMethod.Should().BeFalse();
            corsPolicy.AllowAnyHeader.Should().Be(allowHeader);
            corsPolicy.Origins.Should().Contain("http://localhost:4200");
            corsPolicy.Origins.Should().Contain("http://localhost:4201");
            corsPolicy.Methods.Should().Contain("POST");
            corsPolicy.SupportsCredentials.Should().BeTrue();
        }
    }
}
