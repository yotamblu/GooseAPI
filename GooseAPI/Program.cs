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
            // 🔴 Load .env BEFORE anything else
            Env.Load();

            var builder = WebApplication.CreateBuilder(args);

            // 🔑 Shared signing key WITH KeyId (THIS FIXES IDX10503)
            var signingKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!)
            )
            {
                KeyId = "goosenet-default"
            };

            builder.Services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    // 🔴 Required for .NET 8 compatibility
                    options.UseSecurityTokenValidators = true;

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,

                        ValidIssuer = builder.Configuration["Jwt:Issuer"],
                        ValidAudiences = new[]
                        {
                            builder.Configuration["Jwt:Audience"]
                        },

                        IssuerSigningKey = signingKey,

                        // 🔴 This makes User.Identity.Name work
                        NameClaimType = ClaimTypes.NameIdentifier,

                        ClockSkew = TimeSpan.Zero
                    };

                    // 🔍 DEBUG LOGGING (you can remove later)
                    options.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = context =>
                        {
                            Console.WriteLine("❌ JWT AUTH FAILED:");
                            Console.WriteLine(context.Exception);
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

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseCors("AllowAll");

            // 🔴 ORDER MATTERS
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
            app.Run();
        }
    }
}
