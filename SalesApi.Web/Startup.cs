﻿using System.IO;
using System.Reflection;
using AutoMapper;
using FluentValidation.AspNetCore;
using Infrastructure.Features.Data;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using SalesApi.DataContext.Contexts;
using SalesApi.Shared.Settings;
using SalesApi.ViewModels.Retail;
using SalesApi.Web.Configurations;
using Swashbuckle.AspNetCore.Swagger;

namespace SalesApi.Web
{
    public class Startup
    {
        public static IConfiguration Configuration { get; private set; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<SalesContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

            services.AddMvc(options =>
            {
                options.OutputFormatters.Remove(new XmlDataContractSerializerOutputFormatter());

                // set authorize on all controllers or routes
                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
                options.Filters.Add(new AuthorizeFilter(policy));
            })
            .AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<RetailPromotionSeriesAddViewModelValidator>());

            services.AddAutoMapper();

            services.AddScoped<IUnitOfWork, SalesContext>();
            services.AddScoped(typeof(ICoreService<>), typeof(CoreService<>));

            services.AddRepositories();

            var physicalProvider = new PhysicalFileProvider(Directory.GetCurrentDirectory());
            var embeddedProvider = new EmbeddedFileProvider(Assembly.GetEntryAssembly());
            var compositeProvider = new CompositeFileProvider(physicalProvider, embeddedProvider);
            services.AddSingleton<IFileProvider>(compositeProvider);

            services.AddServices();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info { Title = SalesApiSettings.ApiDisplayName, Version = "v1" });
            });

            services.AddMvcCore()
                .AddAuthorization()
                .AddJsonFormatters();

            services.AddAuthentication("Bearer")
                .AddIdentityServerAuthentication(options =>
                {
                    options.Authority = Configuration["MLH:AuthorizationServer:ServerBase"];
                    options.RequireHttpsMetadata = false;

                    options.ApiName = SalesApiSettings.ApiName;
                });

            services.AddCors(options =>
            {
                options.AddPolicy(SalesApiSettings.CorsPolicyName, policy =>
                {
                    policy.WithOrigins(Configuration["MLH:SalesApi:ClientBase"])
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
#if !DEBUG
            app.InitializeDatabase();
#endif
            app.UseExceptionHandlingMiddleware();
            app.UseCors(SalesApiSettings.CorsPolicyName);
            app.UseStaticFiles();
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", SalesApiSettings.ClientName + " API v1");
            });
            app.UseAuthentication();
            app.UseMvc();
        }
    }
}
