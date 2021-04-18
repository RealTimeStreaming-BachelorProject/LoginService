using System;
using System.Text;
using System.Threading.Tasks;
using Cassandra;
using LoginService.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
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
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "LoginService", Version = "v1" });
            });

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
            services.AddScoped<AuthRepo>();

            var cluster = Cluster.Builder()
                .AddContactPoints(environmentVariables.DatabaseHost)
                .WithPort(environmentVariables.DatabasePort)
                .Build();

            services.AddSingleton(cluster);
        }

        public EnvironmentVariables CheckEnvironmentVariables()
        {
            if (_appenv.IsDevelopment())
            {
                return new EnvironmentVariables
                {
                    DatabaseHost = "127.0.0.1",
                    DatabasePort = 9042,
                    JwtIssuerAuthorithy = "https://localhost:5005",
                    JwtKey = "developmentjwtkey"
                };
            }

            Console.ForegroundColor = ConsoleColor.Red;

            // environment variables check
            var connectionHost = Environment.GetEnvironmentVariable("LOGINSERVICE_DATABASE_HOST");
            if (connectionHost == null)
            {
                Console.WriteLine("'LOGINSERVICE_DATABASE_HOST' environment variable not found");
                Environment.Exit(1);
            }
            var connectionPort = int.Parse(Environment.GetEnvironmentVariable("LOGINSERVICE_DATABASE_PORT"));

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
                DatabaseHost = connectionHost,
                DatabasePort = connectionPort,
                JwtIssuerAuthorithy = jwtIssuerAuthorithy,
                JwtKey = jwtKey
            };
        }

        public struct EnvironmentVariables
        {
            public string DatabaseHost { get; init; }
            public int DatabasePort { get; init; }
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
            app.UseRouting();

            app.UseCors();

            app.UseAuthorization();

            app.UseAuthentication();

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}