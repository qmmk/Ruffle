using Backend.DataAccess;
using Backend.Interface;
using Backend.Manager;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Ruffle
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var appSettingsSection = Configuration.GetSection("AppSettings");
            var appSettings = appSettingsSection.Get<AppSettings>();
            services.Configure<AppSettings>(Configuration.GetSection("AppSettings"));

            services.AddControllers();

            services.AddDbContext<DataContext>(options =>
                  options.UseSqlServer(appSettings.ConnectionString, o =>
                  {
                      o.EnableRetryOnFailure();
                  })
             );

            services.AddScoped<IServiceManager, ServiceManager>();

            var key = Encoding.ASCII.GetBytes(appSettings.JwtTokenSecret);
            
            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(x =>
            {
                x.RequireHttpsMetadata = false;
                x.SaveToken = true;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                };

                // Event handler del token signalr
                x.Events = new JwtBearerEvents()
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];

                        // If the request is for our hub...
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && (path.StartsWithSegments("/hub")))
                        {
                            // Read the token out of the query string
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            }).AddGoogle(opt => {
                IConfigurationSection googleAuthNSection = Configuration.GetSection("Authentication:Google");

                opt.ClientId = googleAuthNSection["ClientId"];
                opt.ClientSecret = googleAuthNSection["ClientSecret"];
            });
            
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder => { builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod(); });

                options.AddPolicy("hubPolicy", builder => {
                    builder.WithOrigins("https://surveyswebapplication20200921103451.azurewebsites.net", "https://localhost:44301").AllowAnyMethod().AllowAnyHeader().AllowCredentials();
                });
            });

            services.AddSignalR(hubOptions =>
            {
                hubOptions.EnableDetailedErrors = true;
            });

            services.AddSingleton<IUserIdProvider, UserIdProvider>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseCors();

            app.UseHttpsRedirection();

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                // API REST --> [controller]/[action]
                endpoints.MapControllers();
                endpoints.MapHub<HubManager>("/hub").RequireCors("hubPolicy");
            });
        }
    }
}
