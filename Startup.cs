using System;
using System.Text;
using System.Threading.Tasks;
using LoginService.Data;
using LoginService.Data.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace LoginService
{
    public class Startup
    {
        public static EnvironmentVariables environmentVariables;

        // We use a key generated on this server during startup to secure our JSON Web Tokens.
        // This means that if the app restarts, existing tokens become invalid.
        public static SymmetricSecurityKey SecurityKey;
        private readonly IWebHostEnvironment _appenv;

        public Startup(IConfiguration configuration, IWebHostEnvironment appEnv)
        {
            Configuration = configuration;
            _appenv = appEnv;
            environmentVariables = CheckEnvironmentVariables();
            SecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(environmentVariables.JwtKey));
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // allow all cors
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder =>
                {
                    // Any origin is allowed
                    builder.AllowAnyMethod().AllowAnyHeader().SetIsOriginAllowed((host) => true).AllowCredentials();
                });
            });

            services.AddControllers();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo {Title = "LoginService", Version = "v1"});
            });

            services.AddDbContext<DataContext>(options =>
            {
                options.UseNpgsql(environmentVariables.DatabaseConnectStr, options => options.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null
                ));
            });

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<DataContext>()
                .AddDefaultTokenProviders();

            // Adding Authentication  
            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options => // Adding Jwt Bearer
                {
                    options.SaveToken = true;
                    options.RequireHttpsMetadata = false;
                    options.TokenValidationParameters = new TokenValidationParameters()
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidAudience = environmentVariables.JwtIssuerAuthorithy,
                        ValidIssuer = environmentVariables.JwtIssuerAuthorithy,
                        IssuerSigningKey = SecurityKey
                    };
                });
            services.AddScoped<ApplicationUserRepo>();
        }

        public EnvironmentVariables CheckEnvironmentVariables()
        {
            if (_appenv.IsDevelopment())
            {
                return new EnvironmentVariables
                {
                    DatabaseConnectStr =
                        "Host=localhost;Port=5432;Database=loginservice-database;Username=loginservice;Password=Loginservice_database_password1",
                    JwtIssuerAuthorithy = "https://localhost:5005",
                    JwtKey = "developmentjwtkey"
                };
            }

            Console.ForegroundColor = ConsoleColor.Red;

            // environment variables check
            var connectionString = Environment.GetEnvironmentVariable("LOGINSERVICE_POSTGRES_CONNECTION_STRING");
            if (connectionString == null)
            {
                Console.WriteLine("'LOGINSERVICE_POSTGRES_CONNECTION_STRING' Database Connection string not found");
                Environment.Exit(1);
            }

            var jwtIssuerAuthorithy = Environment.GetEnvironmentVariable("LOGINSERVICE_JWT_ISSUER");
            if (jwtIssuerAuthorithy == null)
            {
                Console.WriteLine("'LOGINSERVICE_JWT_ISSUER' not found");
                Environment.Exit(1);
            }

            var jwtKey = Environment.GetEnvironmentVariable("LOGINSERVICE_JWT_KEY");
            if (jwtKey == null)
            {
                Console.WriteLine("'LOGINSERVICE_JWT_KEY' not found");
                Environment.Exit(1);
            }

            else
            {
                if (jwtKey.Length < 16)
                {
                    Console.WriteLine("'LOGINSERVICE_JWT_KEY' must be atleast 16 characters long");
                    Environment.Exit(1);
                }
            }

            Console.ResetColor();
            return new EnvironmentVariables
            {
                DatabaseConnectStr = connectionString,
                JwtIssuerAuthorithy = jwtIssuerAuthorithy,
                JwtKey = jwtKey
            };
        }

        public struct EnvironmentVariables
        {
            public string DatabaseConnectStr { get; init; }
            public string JwtIssuerAuthorithy { get; init; }
            public string JwtKey { get; init; }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "LoginService v1");
                    c.RoutePrefix = string.Empty;
                });
            }

            // await UpdateDatabase(app, logger);

            app.UseRouting();

            app.UseCors();

            app.UseAuthorization();

            app.UseAuthentication();

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }

        // Ensures an updated database to the latest migration
        private static async Task UpdateDatabase(IApplicationBuilder app, ILogger<Startup> logger)
        {
            using var serviceScope = app.ApplicationServices
                .GetRequiredService<IServiceScopeFactory>()
                .CreateScope();

            await using var context = serviceScope.ServiceProvider.GetService<DataContext>();
            // Npgsql resiliency strategy does not work with Database.EnsureCreated() and Database.Migrate().
            // Therefore a retry pattern is implemented for this purpose 
            // if database connection is not ready it will retry 3 times before finally quiting
            const int retryCount = 3;
            var currentRetry = 0;
            while (true)
            {
                try
                {
                    logger.LogInformation("Attempting database migration");

                    context.Database.Migrate();

                    logger.LogInformation("Database migration & connection successful");

                    break; // just break if migration is successful
                }
                catch (Npgsql.NpgsqlException)
                {
                    logger.LogError("Database migration failed. Retrying in 5 seconds ...");

                    currentRetry++;

                    if (currentRetry == retryCount
                    ) // Here it is possible to check the type of exception if needed with an OR. And exit if it's a specific exception.
                    {
                        // We have tried as many times as retryCount specifies. Now we throw it and exit the application
                        logger.LogCritical($"Database migration failed after {retryCount} retries");
                        throw;
                    }
                }

                // Waiting 5 seconds before trying again
                await Task.Delay(TimeSpan.FromSeconds(5));
            }
        }
    }
}