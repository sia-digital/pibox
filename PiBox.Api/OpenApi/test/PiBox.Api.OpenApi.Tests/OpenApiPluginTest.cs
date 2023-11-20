using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using PiBox.Api.OpenApi.Filters.Document;
using PiBox.Api.OpenApi.Filters.Schema;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace PiBox.Api.OpenApi.Tests
{
    public class OpenApiPluginTest
    {
        private readonly IHostEnvironment _environment = Substitute.For<IHostEnvironment>();

        private OpenApiPlugin GetPlugin(OpenApiConfiguration configuration)
        {
            return new OpenApiPlugin(configuration, _environment);
        }

        [Test]
        public void SwaggerUiUsesExampleDesignByDefault()
        {
            _environment.ApplicationName.Returns("AwesomeApp");
            var applicationBuilder = Substitute.For<IApplicationBuilder>();
            var configuration = new OpenApiConfiguration();
            var services = new ServiceCollection();
            var swaggerOptions = Substitute.For<IOptionsSnapshot<SwaggerOptions>>();
            swaggerOptions.Value.Returns(new SwaggerOptions());
            var swaggerUiOptions = Substitute.For<IOptionsSnapshot<SwaggerUIOptions>>();
            swaggerUiOptions.Value.Returns(new SwaggerUIOptions());
            services.AddSingleton(swaggerOptions);
            services.AddSingleton(swaggerUiOptions);
            var serviceProvider = services.BuildServiceProvider();
            applicationBuilder.ApplicationServices.Returns(serviceProvider);
            var plugin = GetPlugin(configuration);
            plugin.ConfigureApplication(applicationBuilder);

            swaggerUiOptions.Value.RoutePrefix.Should().BeEmpty();
            swaggerUiOptions.Value.ConfigObject.DisplayRequestDuration.Should().BeTrue();
            swaggerUiOptions.Value.ConfigObject.Urls.Should().Contain(x => x.Url == "swagger/v1/swagger.json" && x.Name == "AwesomeApp");
        }

        [Test]
        public void SwaggerUiCanServeWithoutExampleCss()
        {
            _environment.ApplicationName.Returns("AwesomeApp");
            var applicationBuilder = Substitute.For<IApplicationBuilder>();
            var configuration = new OpenApiConfiguration();
            var services = new ServiceCollection();
            var swaggerOptions = Substitute.For<IOptionsSnapshot<SwaggerOptions>>();
            swaggerOptions.Value.Returns(new SwaggerOptions());
            var swaggerUiOptions = Substitute.For<IOptionsSnapshot<SwaggerUIOptions>>();
            swaggerUiOptions.Value.Returns(new SwaggerUIOptions());
            services.AddSingleton(swaggerOptions);
            services.AddSingleton(swaggerUiOptions);
            var serviceProvider = services.BuildServiceProvider();
            applicationBuilder.ApplicationServices.Returns(serviceProvider);
            var plugin = GetPlugin(configuration);
            plugin.ConfigureApplication(applicationBuilder);

            swaggerUiOptions.Value.HeadContent.Should().NotContain("swagger/custom.css");
            swaggerUiOptions.Value.RoutePrefix.Should().BeEmpty();
            swaggerUiOptions.Value.ConfigObject.DisplayRequestDuration.Should().BeTrue();
            swaggerUiOptions.Value.ConfigObject.Urls.Should().Contain(x => x.Url == "swagger/v1/swagger.json" && x.Name == "AwesomeApp");
        }

        [Test]
        public void SwashbuckleIsSetupCorrectly()
        {
            _environment.ApplicationName.Returns("MyApp");
            var services = new ServiceCollection();
            services.AddSingleton(Substitute.For<IWebHostEnvironment>());
            var configuration = new OpenApiConfiguration();
            var plugin = GetPlugin(configuration);
            plugin.ConfigureServices(services);
            var sp = services.BuildServiceProvider()!;
            var swaggerGenOptions = sp.GetService<IOptions<SwaggerGenOptions>>()?.Value!;

            swaggerGenOptions.Should().NotBeNull();

            var openApiDoc = swaggerGenOptions!.SwaggerGeneratorOptions.SwaggerDocs["v1"];
            openApiDoc.Title.Should().Be("MyApp");
            openApiDoc.Version.Should().Be("v1");

            swaggerGenOptions.SchemaFilterDescriptors.Should().Contain(x => x.Type == typeof(ReadOnlyPropertySchemaFilter));

            swaggerGenOptions.SchemaFilterDescriptors.Should().Contain(x => x.Type == typeof(ValueObjectSchemaFilter));
            swaggerGenOptions.DocumentFilterDescriptors.Should().Contain(x => x.Type == typeof(ValueObjectDocumentFilter));

            swaggerGenOptions.SchemaFilterDescriptors.Should().Contain(x => x.Type == typeof(AnnotationsSchemaFilter));
            swaggerGenOptions.ParameterFilterDescriptors.Should()
                .Contain(x => x.Type == typeof(AnnotationsParameterFilter));
            swaggerGenOptions.OperationFilterDescriptors.Should()
                .Contain(x => x.Type == typeof(AnnotationsOperationFilter));
            swaggerGenOptions.DocumentFilterDescriptors.Should()
                .Contain(x => x.Type == typeof(AnnotationsDocumentFilter));
            swaggerGenOptions.RequestBodyFilterDescriptors.Should()
                .Contain(x => x.Type == typeof(AnnotationsRequestBodyFilter));

            swaggerGenOptions.SchemaFilterDescriptors.Should().Contain(x => x.Type == typeof(XmlCommentsSchemaFilter));
            swaggerGenOptions.ParameterFilterDescriptors.Should()
                .Contain(x => x.Type == typeof(XmlCommentsParameterFilter));
            swaggerGenOptions.OperationFilterDescriptors.Should()
                .Contain(x => x.Type == typeof(XmlCommentsOperationFilter));
            swaggerGenOptions.RequestBodyFilterDescriptors.Should()
                .Contain(x => x.Type == typeof(XmlCommentsRequestBodyFilter));
        }

        [Test]
        public void AuthenticationCanBeEnabledAndUsed()
        {
            var configuration = new OpenApiConfiguration
            {
                Auth = new OpenApiConfiguration.OpenApiAuthConfiguration
                {
                    TokenUrl = "https://example.com/token",
                    AuthUrl = "https://example.com/auth",
                    Enabled = true
                }
            };
            var authUrl = new Uri("https://example.com/auth");
            var tokenUrl = new Uri("https://example.com/token");
            var sc = new ServiceCollection();
            var plugin = GetPlugin(configuration);
            plugin.ConfigureServices(sc);
            var sp = sc.BuildServiceProvider()!;
            var swaggerGenOptions = sp.GetService<IOptions<SwaggerGenOptions>>()?.Value!;

            swaggerGenOptions.Should().NotBeNull();
            var securityRequirement = swaggerGenOptions!.SwaggerGeneratorOptions.SecurityRequirements.FirstOrDefault();
            securityRequirement.Should().NotBeNull();

            var securitySchema = swaggerGenOptions.SwaggerGeneratorOptions.SecuritySchemes.First().Value;
            securitySchema.Should().NotBeNull();
            securitySchema.Flows.Implicit.Should().NotBeNull();
            securitySchema.Flows.Implicit.AuthorizationUrl.Should().Be(authUrl);
            securitySchema.Flows.AuthorizationCode.Should().NotBeNull();
            securitySchema.Flows.AuthorizationCode.TokenUrl.Should().Be(tokenUrl);
            securitySchema.Flows.AuthorizationCode.AuthorizationUrl.Should().Be(authUrl);
            securitySchema.Flows.Password.Should().NotBeNull();
            securitySchema.Flows.Password.TokenUrl.Should().Be(tokenUrl);
            securitySchema.Flows.ClientCredentials.Should().NotBeNull();
            securitySchema.Flows.ClientCredentials.TokenUrl.Should().Be(tokenUrl);
        }
    }
}
