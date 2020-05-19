﻿using System.Collections.Generic;
using System.Threading.Tasks;
using AcceptanceTests.Common.Configuration;
using AcceptanceTests.Common.Configuration.Users;
using AcceptanceTests.Common.Data.TestData;
using AdminWebsite.AcceptanceTests.Configuration;
using AdminWebsite.AcceptanceTests.Data;
using AdminWebsite.AcceptanceTests.Data.TestData;
using AdminWebsite.AcceptanceTests.Helpers;
using AdminWebsite.BookingsAPI.Client;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using TechTalk.SpecFlow;
using HearingDetails = AdminWebsite.AcceptanceTests.Data.HearingDetails;

namespace AdminWebsite.AcceptanceTests.Hooks
{
    [Binding]
    public sealed class ConfigHooks
    {
        private readonly IConfigurationRoot _configRoot;

        public ConfigHooks(TestContext context)
        {
            _configRoot = ConfigurationManager.BuildConfig("f99a3fe8-cf72-486a-b90f-b65c27da84ee", GetTargetEnvironment(), RunOnSauceLabsFromLocal());
            context.AdminWebConfig = new AdminWebConfig();
            context.UserAccounts = new List<UserAccount>();
            context.Tokens = new AdminWebTokens();
        }

        private static string GetTargetEnvironment()
        {
            return NUnit.Framework.TestContext.Parameters["TargetEnvironment"] ?? "";
        }

        private static bool RunOnSauceLabsFromLocal()
        {
            return NUnit.Framework.TestContext.Parameters["RunOnSauceLabs"] != null &&
                   NUnit.Framework.TestContext.Parameters["RunOnSauceLabs"].Equals("true");
        }

        [BeforeScenario(Order = (int)HooksSequence.ConfigHooks)]
        public async Task RegisterSecrets(TestContext context)
        {
            RegisterAzureSecrets(context);
            RegisterTestUserSecrets(context);
            RegisterTestUsers(context);
            RegisterDefaultData(context);
            RegisterHearingServices(context);
            RegisterWowzaSettings(context);
            RegisterSauceLabsSettings(context);
            RunningAdminWebLocally(context);
            await GenerateBearerTokens(context);
        }

        private void RegisterAzureSecrets(TestContext context)
        {
            context.AdminWebConfig.AzureAdConfiguration = Options.Create(_configRoot.GetSection("AzureAd").Get<AdminWebSecurityConfiguration>()).Value;
            ConfigurationManager.VerifyConfigValuesSet(context.AdminWebConfig.AzureAdConfiguration);
        }

        private void RegisterTestUserSecrets(TestContext context)
        {
            context.AdminWebConfig.TestConfig = Options.Create(_configRoot.GetSection("TestUserSecrets").Get<AdminWebTestConfig>()).Value;
            ConfigurationManager.VerifyConfigValuesSet(context.AdminWebConfig.TestConfig);
        }

        private void RegisterTestUsers(TestContext context)
        {
            context.UserAccounts = Options.Create(_configRoot.GetSection("UserAccounts").Get<List<UserAccount>>()).Value;
            context.UserAccounts.Should().NotBeNullOrEmpty();
            foreach (var user in context.UserAccounts)
            {
                user.Key = user.Lastname;
                user.Username = $"{user.DisplayName.Replace(" ", "").Replace("ClerkJudge", "Clerk")}{context.AdminWebConfig.TestConfig.TestUsernameStem}";
            }
        }

        private static void RegisterDefaultData(TestContext context)
        {
            context.Test = new Test
            {
                AddParticipant = new AddParticipant(),
                AssignJudge = new AssignJudge(),
                CommonData = LoadXmlFile.SerialiseCommonData(),
                HearingDetails = new HearingDetails(),
                HearingParticipants = new List<UserAccount>(),
                HearingResponse = new HearingDetailsResponse(),
                HearingSchedule = new HearingSchedule(),
                TestData = new DefaultDataManager().SerialiseTestData()
            };
            context.Test.AddParticipant = context.Test.TestData.AddParticipant;
        }

        private void RegisterHearingServices(TestContext context)
        {
            context.AdminWebConfig.VhServices = Options.Create(_configRoot.GetSection("VhServices").Get<AdminWebVhServiceConfig>()).Value;
            ConfigurationManager.VerifyConfigValuesSet(context.AdminWebConfig.VhServices);
        }

        private void RegisterWowzaSettings(TestContext context)
        {
            context.AdminWebConfig.Wowza = Options.Create(_configRoot.GetSection("WowzaConfiguration").Get<WowzaConfiguration>()).Value;
            ConfigurationManager.VerifyConfigValuesSet(context.AdminWebConfig.Wowza);
        }

        private void RegisterSauceLabsSettings(TestContext context)
        {
            context.AdminWebConfig.SauceLabsConfiguration = Options.Create(_configRoot.GetSection("Saucelabs").Get<SauceLabsSettingsConfig>()).Value;
            if (context.AdminWebConfig.SauceLabsConfiguration.RunningOnSauceLabs())
                context.AdminWebConfig.SauceLabsConfiguration.SetRemoteServerUrlForDesktop(context.Test.CommonData.CommonConfig.SauceLabsServerUrl);
        }

        private static void RunningAdminWebLocally(TestContext context)
        {
            context.AdminWebConfig.VhServices.RunningAdminWebLocally = context.AdminWebConfig.VhServices.AdminWebUrl.Contains("localhost");
        }

        private static async Task GenerateBearerTokens(TestContext context)
        {
            context.Tokens.BookingsApiBearerToken = await ConfigurationManager.GetBearerToken(
                context.AdminWebConfig.AzureAdConfiguration, context.AdminWebConfig.VhServices.BookingsApiResourceId);
            context.Tokens.BookingsApiBearerToken.Should().NotBeNullOrEmpty();

            context.Tokens.UserApiBearerToken = await ConfigurationManager.GetBearerToken(
                context.AdminWebConfig.AzureAdConfiguration, context.AdminWebConfig.VhServices.UserApiResourceId);
            context.Tokens.UserApiBearerToken.Should().NotBeNullOrEmpty();

            context.Tokens.VideoApiBearerToken = await ConfigurationManager.GetBearerToken(
                context.AdminWebConfig.AzureAdConfiguration, context.AdminWebConfig.VhServices.VideoApiResourceId);
            context.Tokens.VideoApiBearerToken.Should().NotBeNullOrEmpty();
        }
    }
}
