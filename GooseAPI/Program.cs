using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace GooseAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            AppContext.SetSwitch("System.Drawing.EnableUnixSupport", true);
            // 🔴 1. Load .env BEFORE anything else
            // If running via systemd, this looks in the WorkingDirectory
            Env.Load();

            var builder = WebApplication.CreateBuilder(args);

            // 🛡️ 2. FAIL-SAFE: Verify JWT configuration exists
            var jwtSecret = builder.Configuration["Jwt:Secret"];
            if (string.IsNullOrEmpty(jwtSecret))
            {
                Console.WriteLine("**************************************************");
                Console.WriteLine("CRITICAL ERROR: 'Jwt:Secret' is missing!");
                Console.WriteLine("Check if your .env file exists in the root folder.");
                Console.WriteLine("Current Directory: " + Directory.GetCurrentDirectory());
                Console.WriteLine("**************************************************");
                // Stop the app immediately so you don't chase ghost bugs
                Environment.Exit(1);
            }

            // 🔑 3. Shared signing key WITH KeyId (Fixed IDX10503)
            var signingKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSecret)
            )
            {
                KeyId = "goosenet-default"
            };

            // 🏗️ 4. Configure Services
            builder.Services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    // Required for .NET 8 compatibility
                    options.UseSecurityTokenValidators = true;

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,

                        ValidIssuer = builder.Configuration["Jwt:Issuer"],
                        ValidAudiences = new[] { builder.Configuration["Jwt:Audience"] },

                        IssuerSigningKey = signingKey,

                        // Makes User.Identity.Name work with the ID claim
                        NameClaimType = ClaimTypes.NameIdentifier,
                        ClockSkew = TimeSpan.Zero
                    };

                    // 🔍 DEBUG LOGGING
                    options.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = context =>
                        {
                            Console.WriteLine($"❌ JWT AUTH FAILED: {context.Exception.Message}");
                            return Task.CompletedTask;
                        },
                        OnTokenValidated = context =>
                        {
                            Console.WriteLine("✅ JWT VALIDATED");
                            return Task.CompletedTask;
                        }
                    };
                });

            builder.Services.AddAuthorization();
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            var app = builder.Build();

            // 🚀 5. Configure Middleware Pipeline
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseCors("AllowAll");

            // 🔴 ORDER MATTERS: Auth must come before MapControllers
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            Console.WriteLine("🦆 GooseNet API is starting up...");
            app.Run();
        }
    }
}