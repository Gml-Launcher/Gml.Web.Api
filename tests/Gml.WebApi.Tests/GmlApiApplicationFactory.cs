using Gml.Web.Api.Core.Middlewares;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;  // For configuration builder
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;  // For dictionary

namespace Gml.WebApi.Tests;

internal class GmlApiApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
            {
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "TestScheme";
                    options.DefaultScheme = "TestScheme";
                    options.DefaultChallengeScheme = "TestScheme";
                }).AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>("TestScheme", options => { });
            })
            .ConfigureAppConfiguration((context, config) =>
            {
                var configuration = new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string>
                    {
                        {"SECURITY_KEY", "jkuhbsfgvuk4gfikhn8i7wa34rkbqw23"},
                        {"PROJECT_NAME", "GmlServer"},
                        {"PROJECT_DESCRIPTION", "GmlServer Description"},
                        {"PROJECT_POLICYNAME", "GmlPolicy"},
                        {"PROJECT_PATH", ""}
                    }!)
                    .Build();

                config.AddConfiguration(configuration);
            });
    }
}
