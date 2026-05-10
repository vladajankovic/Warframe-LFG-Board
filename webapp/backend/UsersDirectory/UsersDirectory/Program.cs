
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
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
            builder.Services.AddScoped<IPasswordHasher<AddUserCommand>, PasswordHasher<AddUserCommand>>();

            builder.Services.AddTransient<IValidator<AddUserCommand>, AddUserCommandValidator>();

            builder.Services.AddScoped<DbSeed>();

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

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
