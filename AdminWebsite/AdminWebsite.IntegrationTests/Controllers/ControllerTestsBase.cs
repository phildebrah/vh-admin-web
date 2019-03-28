﻿using System;
using AdminWebsite.Configuration;
using AdminWebsite.Security;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Microsoft.Extensions.PlatformAbstractions;
using System.IO;

namespace AdminWebsite.IntegrationTests.Controllers
{
    [Parallelizable(ParallelScope.All)]
    public class ControllerTestsBase
    {
        private TestServer _server;
        private string _bearerToken = String.Empty;
        protected string GraphApiToken;   
        private readonly string _environmentName = "Development";
        
        protected ControllerTestsBase()
        {
            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")))
            {
                _environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            }
        }
        
        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            var integrationTestsPath = PlatformServices.Default.Application.ApplicationBasePath;
            var applicationPath = Path.GetFullPath(Path.Combine(integrationTestsPath, "../../../../AdminWebsite"));
            var webHostBuilder =
                WebHost.CreateDefaultBuilder()
                    .UseContentRoot(applicationPath)
                    .UseWebRoot(applicationPath)
                    .UseEnvironment("Development")
                    .UseKestrel(c => c.AddServerHeader = false)
                    .UseStartup<Startup>();
            _server = new TestServer(webHostBuilder);
            GetClientAccessTokenForBookHearingApi();
        }

        private void GetClientAccessTokenForBookHearingApi()
        {
            var configRootBuilder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddUserSecrets<Startup>();
            
            var configRoot = configRootBuilder.Build();

            var securitySettingsOptions = Options.Create(configRoot.GetSection("AzureAd").Get<SecuritySettings>());
            var serviceSettingsOptions = Options.Create(configRoot.GetSection("VhServices").Get<ServiceSettings>());
            var securitySettings = securitySettingsOptions.Value;
            var serviceSettings = serviceSettingsOptions.Value;
            _bearerToken = new TokenProvider(securitySettingsOptions).GetClientAccessToken(
                securitySettings.ClientId, securitySettings.ClientSecret,
                securitySettings.ClientId);

            GraphApiToken = new TokenProvider(securitySettingsOptions).GetClientAccessToken(
                securitySettings.ClientId, securitySettings.ClientSecret,
                "https://graph.microsoft.com");
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _server.Dispose();
        }
        
        protected async Task<HttpResponseMessage> SendGetRequestAsync(string uri)
        {
            using (var client = _server.CreateClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_bearerToken}");
                return await client.GetAsync(uri);
            }
        }

        protected async Task<HttpResponseMessage> SendPostRequestAsync(string uri, HttpContent httpContent)
        {
            using (var client = _server.CreateClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_bearerToken}");
                return await client.PostAsync(uri, httpContent);
            }
        }

        protected async Task<HttpResponseMessage> SendPatchRequestAsync(string uri, StringContent httpContent)
        {
            using (var client = _server.CreateClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_bearerToken}");
                return await client.PatchAsync(uri, httpContent);
            }
        }

        protected async Task<HttpResponseMessage> SendPutRequestAsync(string uri, StringContent httpContent)
        {
            using (var client = _server.CreateClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_bearerToken}");
                return await client.PutAsync(uri, httpContent);
            }
        }

        protected async Task<HttpResponseMessage> SendDeleteRequestAsync(string uri)
        {
            using (var client = _server.CreateClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_bearerToken}");
                return await client.DeleteAsync(uri);
            }
        }
    }
}