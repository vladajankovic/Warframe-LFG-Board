
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RabbitMQ.Client;
using SharpGrip.FluentValidation.AutoValidation.Mvc.Extensions;
using System.Reflection;
using System.Text;
using UsersDirectory.Broker.RabbitMQ;
using UsersDirectory.Commands;
using UsersDirectory.Database;
using UsersDirectory.Repositories;
using UsersDirectory.Seed;
using UsersDirectory.Services;
using UsersDirectory.Utility.Mappers;
using UsersDirectory.Validators;

namespace UsersDirectory
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");
            builder.Services.AddDbContext<UsersDbContext>(options =>
                options.UseNpgsql(connectionString));

            builder.Services.AddAutoMapper(Assembly.GetAssembly(typeof(AutoMapperProfile)));
            builder.Services.AddScoped<IUsersRepository, UsersRepository>();

            builder.Services.AddScoped<IUsersService, UserService>();
            builder.Services.AddScoped<IPasswordHasher<string>, PasswordHasher<string>>();
            builder.Services.AddHostedService<SubscriptionRenewalService>();

            // Configure RabbitMQ
            var rabbitMQHost = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
            var rabbitMQPort = int.TryParse(builder.Configuration["RabbitMQ:Port"], out var port) ? port : 5672;
            var rabbitMQUsername = builder.Configuration["RabbitMQ:Username"] ?? "guest";
            var rabbitMQPassword = builder.Configuration["RabbitMQ:Password"] ?? "guest";

            var connectionFactory = new ConnectionFactory()
            {
                HostName = rabbitMQHost,
                Port = rabbitMQPort,
                UserName = rabbitMQUsername,
                Password = rabbitMQPassword
            };

            builder.Services.AddSingleton<IConnectionFactory>(connectionFactory);
            builder.Services.AddScoped<IRabbitMQPublisher, RabbitMQPublisher>();

            builder.Services.AddTransient<IValidator<AddUserCommand>, AddUserCommandValidator>();
            builder.Services.AddTransient<IValidator<UpdateUserCommand>, UpdateUserCommandValidator>();
            builder.Services.AddTransient<IValidator<ChangePasswordCommand>, ChangePasswordCommandValidator>();

            builder.Services.AddFluentValidationAutoValidation();

            builder.Services.AddScoped<DbSeed>();

            // JWT Authentication
            var jwtKey = builder.Configuration["Jwt:Key"]
                ?? throw new InvalidOperationException("JWT key not configured.");
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = builder.Configuration["Jwt:Issuer"],
                        ValidAudience = builder.Configuration["Jwt:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    };
                });

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            var app = builder.Build();

            // Apply database migrations on startup
            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<UsersDbContext>();
                dbContext.Database.Migrate();

                var dbSeed = scope.ServiceProvider.GetRequiredService<DbSeed>();
                await dbSeed.SeedUsers();
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            if (!app.Environment.IsDevelopment())
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
