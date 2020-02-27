﻿using AdminWebsite.BookingsAPI.Client;
using AdminWebsite.Configuration;
using AdminWebsite.Helper;
using AdminWebsite.Models;
using AdminWebsite.Security;
using AdminWebsite.Services;
using AdminWebsite.Swagger;
using AdminWebsite.UserAPI.Client;
using AdminWebsite.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Swashbuckle.AspNetCore.Swagger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reflection;
using AdminWebsite.Contracts.Responses;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;

namespace AdminWebsite.Extensions
{
    public static class ConfigureServicesExtensions
    {
        public static IServiceCollection AddSwagger(this IServiceCollection serviceCollection)
        {
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

            var contractsXmlFile = $"{typeof(ClientSettingsResponse).Assembly.GetName().Name}.xml";
            var contractsXmlPath = Path.Combine(AppContext.BaseDirectory, contractsXmlFile);

            serviceCollection.AddSwaggerGen(c =>
            {
                c.CustomSchemaIds((type) => type.FullName);
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Book A Hearing Client", Version = "v1" });
                c.AddFluentValidationRules();
                c.IncludeXmlComments(xmlPath);
                c.IncludeXmlComments(contractsXmlPath);
                c.EnableAnnotations();

                c.AddSecurityDefinition("Bearer",
                    new OpenApiSecurityScheme
                    {
                        Description = "Please enter JWT with Bearer into field",
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer"
                    });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement{
                    {
                        new OpenApiSecurityScheme{
                            Reference = new OpenApiReference{
                                Id = "Bearer",
                                Type = ReferenceType.SecurityScheme
                            }
                        },new List<string>()
                    }
                });
                c.OperationFilter<AuthResponsesOperationFilter>();
            });
            serviceCollection.AddSwaggerGenNewtonsoftSupport();

            return serviceCollection;
        }

        public static IServiceCollection AddCustomTypes(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddHttpContextAccessor();
            serviceCollection.AddMemoryCache();
            serviceCollection.AddTransient<HearingApiTokenHandler>();
            serviceCollection.AddTransient<UserApiTokenHandler>();
            serviceCollection.AddScoped<ITokenProvider, TokenProvider>();
            serviceCollection.AddScoped<IUserAccountService, UserAccountService>();
            serviceCollection.AddScoped<SecuritySettings>();
            serviceCollection.AddScoped<AppConfigSettings>();
            serviceCollection.AddSingleton<IClaimsCacheProvider, MemoryClaimsCacheProvider>();
            serviceCollection.AddScoped<ICachedUserClaimBuilder, CachedUserClaimBuilder>();

            // Build the hearings api client using a reusable HttpClient factory and predefined base url
            var container = serviceCollection.BuildServiceProvider();
            var settings = container.GetService<IOptions<ServiceSettings>>().Value;

            serviceCollection.AddHttpClient<IBookingsApiClient, BookingsApiClient>()
                .AddHttpMessageHandler(() => container.GetService<HearingApiTokenHandler>())
                .AddTypedClient(httpClient => BuildHearingApiClient(httpClient, settings));

            serviceCollection.AddHttpClient<IUserApiClient, UserApiClient>()
               .AddHttpMessageHandler(() => container.GetService<UserApiTokenHandler>())
               .AddTypedClient(httpClient => BuildUserApiClient(httpClient, settings));

            serviceCollection.AddTransient<IUserIdentity, UserIdentity>((ctx) =>
            {
                var userPrincipal = ctx.GetService<IHttpContextAccessor>().HttpContext.User;

                return new UserIdentity(userPrincipal);
            });

            serviceCollection.AddSingleton<IValidator<BookNewHearingRequest>, BookNewHearingRequestValidator>();
            serviceCollection.AddSingleton<IValidator<EditHearingRequest>, EditHearingRequestValidator>();

            return serviceCollection;
        }

        private static IBookingsApiClient BuildHearingApiClient(HttpClient httpClient, ServiceSettings serviceSettings)
        {
            return new BookingsApiClient(httpClient) { BaseUrl = serviceSettings.BookingsApiUrl };
        }

        private static IUserApiClient BuildUserApiClient(HttpClient httpClient, ServiceSettings serviceSettings)
        {
            return new UserApiClient(httpClient) { BaseUrl = serviceSettings.UserApiUrl };
        }

        public static IServiceCollection AddJsonOptions(this IServiceCollection serviceCollection)
        {
            var contractResolver = new DefaultContractResolver
            {
                NamingStrategy = new SnakeCaseNamingStrategy()
            };


            serviceCollection.AddMvc()
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.ContractResolver = contractResolver;
                    options.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
                    options.SerializerSettings.Converters.Add(new StringEnumConverter());
                });

            return serviceCollection;
        }
        
        /// <summary>
        /// Temporary work-around until typed-client bug is restored
        /// https://github.com/dotnet/aspnetcore/issues/13346#issuecomment-535544207
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="factory"></param>
        /// <typeparam name="TClient"></typeparam>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        private static IHttpClientBuilder AddTypedClient<TClient>(this IHttpClientBuilder builder,
            Func<HttpClient, TClient> factory)
            where TClient : class
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            builder.Services.AddTransient(s =>
            {
                var httpClientFactory = s.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient(builder.Name);

                return factory(httpClient);
            });

            return builder;
        }
    }
}
