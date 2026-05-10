
using FluentValidation;
using ItemsStore.Commands;
using ItemsStore.Database;
using ItemsStore.Repository;
using ItemsStore.Seed;
using ItemsStore.Services;
using ItemsStore.Utility;
using ItemsStore.Utility.Mappers;
using ItemsStore.Validators;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Reflection;
using System.Text;

namespace ItemsStore
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");
            builder.Services.AddDbContext<ItemsDbContext>(options =>
                options.UseNpgsql(connectionString));

            builder.Services.AddAutoMapper(Assembly.GetAssembly(typeof(AutoMapperProfile)));
            builder.Services.AddScoped<IRelicRepository, RelicRepository>();
            builder.Services.AddScoped<IResourceRepository, ResourceRepository>();

            builder.Services.AddScoped<IRelicService, RelicService>();
            builder.Services.AddScoped<IResourceService, ResourceService>();

            builder.Services.AddTransient<IValidator<AddRelicCommand>, AddRelicCommandValidator>();
            builder.Services.AddTransient<IValidator<AddResourceCommand>, AddResourceCommandValidator>();

            builder.Services.AddHttpClient<WarframeApiUtils>();
            builder.Services.AddScoped<DbSeed>();

            // JWT Authentication (validates tokens issued by UsersDirectory)
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
                var dbContext = scope.ServiceProvider.GetRequiredService<ItemsDbContext>();
                dbContext.Database.Migrate();

                var dbSeed = scope.ServiceProvider.GetRequiredService<DbSeed>();
                await dbSeed.SeedRelics();
                await dbSeed.SeedResources();
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
