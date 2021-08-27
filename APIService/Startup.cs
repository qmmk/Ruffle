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
using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using ServiceStack;
using ServiceStack.Auth;
using Funq;

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

            services.Configure<CookiePolicyOptions>(options =>
            {
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            /*
            services.AddIdentity<ApplicationUser, IdentityRole>(options => {
                options.User.AllowedUserNameCharacters = null;
                        })
                .AddEntityFrameworkStores<DataContext>()
                .AddDefaultTokenProviders();
            */

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
            })
            .AddTwitter(options =>
            { /* Create Twitter App at: https://dev.twitter.com/apps */
                options.ConsumerKey = Configuration["oauth.twitter.ConsumerKey"];
                options.ConsumerSecret = Configuration["oauth.twitter.ConsumerSecret"];
                options.SaveTokens = true;
                options.RetrieveUserDetails = true;
            })
            .AddFacebook(options =>
            { /* Create App https://developers.facebook.com/apps */
                options.AppId = Configuration["oauth.facebook.AppId"];
                options.AppSecret = Configuration["oauth.facebook.AppSecret"];
                options.SaveTokens = true;
                options.Scope.Clear();
                Configuration.GetSection("oauth.facebook.Permissions").GetChildren().ToList().ForEach(x => options.Scope.Add(x.Value));
            })
            .AddGoogle(options =>
            { /* Create App https://console.developers.google.com/apis/credentials */
                options.ClientId = Configuration["oauth.google.ConsumerKey"];
                options.ClientSecret = Configuration["oauth.google.ConsumerSecret"];
                options.SaveTokens = true;
            })
            .AddMicrosoftAccount(options =>
            { /* Create App https://apps.dev.microsoft.com */
                options.ClientId = Configuration["oauth.microsoftgraph.AppId"];
                options.ClientSecret = Configuration["oauth.microsoftgraph.AppSecret"];
                options.SaveTokens = true;
            });

            services.Configure<IdentityOptions>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = false;
                options.Password.RequiredUniqueChars = 6;

                // Lockout settings
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
                options.Lockout.MaxFailedAccessAttempts = 10;
                options.Lockout.AllowedForNewUsers = true;

                // User settings
                options.User.RequireUniqueEmail = true;
            });

            services.ConfigureApplicationCookie(options =>
            {
                // Cookie settings
                options.Cookie.HttpOnly = true;
                options.ExpireTimeSpan = TimeSpan.FromDays(150);
                // If the LoginPath isn't set, ASP.NET Core defaults 
                // the path to /Account/Login.
                options.LoginPath = "/Account/Login";
                // If the AccessDeniedPath isn't set, ASP.NET Core defaults 
                // the path to /Account/AccessDenied.
                options.AccessDeniedPath = "/Account/AccessDenied";
                options.SlidingExpiration = true;
            });

            services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder => { builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod(); });

                options.AddPolicy("hubPolicy", builder =>
                {
                    builder.WithOrigins("https://surveyswebapplication20200921103451.azurewebsites.net", "https://localhost:44301").AllowAnyMethod().AllowAnyHeader().AllowCredentials();
                });
            });

            services.AddSignalR(hubOptions =>
            {
                hubOptions.EnableDetailedErrors = true;
            });

            // services.AddSingleton<IUserIdProvider, UserIdProvider>();

            services.AddTransient<IProviderManager>(c => new ProviderManager(Configuration));
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

            app.UseServiceStack(new AppHost
            {
                AppSettings = new NetCoreAppSettings(Configuration)
            });
        }
    }

    public static class AppExtensions
    {
        public static T DbExec<T>(this IServiceProvider services, Func<System.Data.IDbConnection, T> fn) =>
            services.DbContextExec<DataContext, T>(ctx => {
                ctx.Database.OpenConnection(); return ctx.Database.GetDbConnection();
            }, fn);
    }

    public class AppHost : AppHostBase
    {
        public AppHost() : base("Ruffle", typeof(MyServices).Assembly) { }

        // Configure your AppHost with the necessary configuration and dependencies your App needs
        public override void Configure(Container container)
        {
            SetConfig(new HostConfig
            {
                DebugMode = AppSettings.Get(nameof(HostConfig.DebugMode), false),
                AdminAuthSecret = "adm1nSecret",
            });

            // TODO: Replace OAuth App settings in: appsettings.Development.json
            Plugins.Add(new AuthFeature(() => new CustomUserSession(),
                new IAuthProvider[] {
                    new NetCoreIdentityAuthProvider(AppSettings) // Adapter to enable ASP.NET Core Identity Auth in ServiceStack
                    {
                        AdminRoles = { "Manager" }, // Automatically Assign additional roles to Admin Users
                        PopulateSessionFilter = (session, principal, req) =>
                        {
                            //Example of populating ServiceStack Session Roles + Custom Info from EF Identity DB
                            var user = req.GetMemoryCacheClient().GetOrCreate(
                                IdUtils.CreateUrn(nameof(ApplicationUser), session.Id),
                                TimeSpan.FromMinutes(5), // return cached results before refreshing cache from db every 5 mins
                                () => ApplicationServices.DbExec(db => db.GetIdentityUserById<ApplicationUser>(session.Id)));

                            session.Email = session.Email ?? user.Email;
                            session.FirstName = session.FirstName ?? user.FirstName;
                            session.LastName = session.LastName ?? user.LastName;
                            session.DisplayName = session.DisplayName ?? user.DisplayName;
                            session.ProfileUrl = user.ProfileUrl ?? Svg.GetDataUri(Svg.Icons.DefaultProfile);

                            session.Roles = req.GetMemoryCacheClient().GetOrCreate(
                                IdUtils.CreateUrn(nameof(session.Roles), session.Id),
                                TimeSpan.FromMinutes(5), // return cached results before refreshing cache from db every 5 mins
                                () => ApplicationServices.DbExec(db => db.GetIdentityUserRolesById(session.Id)));
                        }
                    },
                }));
        }
    }

    public class CustomUserSession : AuthUserSession { }
}
