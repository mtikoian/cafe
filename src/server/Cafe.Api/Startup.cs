﻿using AutoMapper;
using Cafe.Api.Configuration;
using Cafe.Api.Filters;
using Cafe.Api.ModelBinders;
using Cafe.Business.AuthContext;
using Cafe.Core.AuthContext;
using Cafe.Core.AuthContext.Commands;
using Cafe.Core.AuthContext.Configuration;
using Cafe.Domain.Entities;
using Cafe.Persistance.EntityFramework;
using FluentValidation.AspNetCore;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Cafe.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<ApplicationDbContext>(opts =>
                opts.UseNpgsql(Configuration.GetConnectionString("DefaultConnection")));

            services.AddAutoMapper();
            services.AddSwagger();

            services.AddJwtIdentity(
                Configuration.GetSection(nameof(JwtConfiguration)),
                options =>
                {
                    options.AddPolicy(AuthConstants.Policies.IsAdmin, pb => pb.RequireClaim(AuthConstants.ClaimTypes.IsAdmin, $"{true}"));
                    options.AddPolicy(AuthConstants.Policies.IsWaiter, pb => pb.RequireClaim(AuthConstants.ClaimTypes.WaiterId));
                    options.AddPolicy(AuthConstants.Policies.IsManager, pb => pb.RequireClaim(AuthConstants.ClaimTypes.ManagerId));
                });

            services.AddLogging(logBuilder => logBuilder.AddSerilog(dispose: true));

            services.AddMarten(Configuration);
            services.AddCqrs();
            services.AddMediatR();

            services.AddTransient<IJwtFactory, JwtFactory>();

            services.AddMvc(options =>
            {
                options.ModelBinderProviders.Insert(0, new OptionModelBinderProvider());
                options.Filters.Add<ExceptionFilter>();
                options.Filters.Add<ModelStateFilter>();
            })
            .AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<RegisterValidator>())
            .SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, ApplicationDbContext dbContext, UserManager<User> userManager)
        {
            if (!env.IsDevelopment())
            {
                app.UseHsts();
            }
            else
            {
                app.AddDefaultAdminAccountIfNoneExisting(dbContext, userManager).Wait();
            }

            loggerFactory.AddLogging(Configuration.GetSection("Logging"));

            app.UseHttpsRedirection();
            app.UseSwagger("Cafe");
            app.UseStaticFiles();
            app.UseAuthentication();
            app.UseMvc();
        }
    }
}
