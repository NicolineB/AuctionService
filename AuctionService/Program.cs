using NLog;
using NLog.Web;
using Repositories;
using Interfaces;
using ConsumerServices;
using ProducerServices;
using AuctionService;
using MongoDB.Driver;
using DBContexts;
using MongoDB.Bson.Serialization.Conventions;
using System.Text.Json.Serialization;
using MongoDB.Bson;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Services;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;

//Initializes logger using NLOG and gets logger instance for current class
var logger = NLog.LogManager.Setup().LoadConfigurationFromAppSettings()
.GetCurrentClassLogger();
logger.Debug("init main");

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Add services to the container.
    builder.Services.AddControllers(config => config.Filters.Add(new ProducesAttribute("application/json")))
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.JsonSerializerOptions.IgnoreNullValues = true; // Optional: Ignore null values in JSON output
        });

    ConventionRegistry.Register("EnumStringConvention", new ConventionPack { new EnumRepresentationConvention(BsonType.String) }, _ => true);

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // Register your repository
    string rmqaddr = Environment.GetEnvironmentVariable("RABBITMQ_HOST");
    builder.Services.AddScoped<IRabbitMQConnectionFactory>(provider =>
                    {
                        // Setting hostname for RabbitMQ server
                        // Return a new RabbitMQConnectionFactory instance
                        return new RabbitMQConnectionFactory(rmqaddr); // Environment.GetEnvironmentVariable("RabbitMQHostName") in docker compose

                    });

    builder.Services.AddHostedService<AuctionScheduler>(); // Add AuctionScheduler
    builder.Services.AddScoped<BidProcessor>(); // Register BidProcessor before BidHandler
    builder.Services.AddScoped<IBidHandler, BidHandler>(); // Register BidHandler
    builder.Services.AddHostedService<RabbitMQConsumer>(); //Add RabbitmqConsumer 
    builder.Services.AddScoped<ILegalRepository, LegalRepository>();  // Add Legal
    builder.Services.AddScoped<IAuctionRepository, AuctionRepository>(); // Add auction 

    // Retrieve the connection string from environment variables or use default value
    var connectionString = Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING");
    var mongoClient = new MongoClient(connectionString);

    // Add MongoDBContext to the service container
    builder.Services.AddSingleton<IMongoDBContext>(s =>
    {
        // Retrieve the database name from environment variables or use default value
        var databaseName = Environment.GetEnvironmentVariable("MONGODB_DATABASE_NAME");
        return new MongoDBContext(mongoClient, databaseName); //Return a new instance of MongoDBContext
    });

    builder.Services.AddScoped<IRabbitMQProducer>(provider =>
    {
        var factory = provider.GetRequiredService<IRabbitMQConnectionFactory>();
        var logger = provider.GetRequiredService<ILogger<RabbitMQProducer>>(); // ILogger for RabbitMQProducer

        return new RabbitMQProducer(factory, logger);
    });

    builder.Logging.ClearProviders();
    builder.Host.UseNLog(); //Nlog setup for dependency injection

    // Add VaultService to the service container
    var vaultToken = Environment.GetEnvironmentVariable("VAULT_TOKEN") ?? throw new ArgumentNullException("VAULT_TOKEN environment variable is not set.");
    var vaultEndPoint = Environment.GetEnvironmentVariable("VAULT_ADDR") ?? throw new ArgumentNullException("VAULT_ENDPOINT environment variable is not set.");
    var vaultService = new VaultService(vaultToken, vaultEndPoint);
    var (secret, issuer) = await vaultService.GetSecretAndIssuerAsync();

    //Add authentication 
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = issuer,
                ValidAudience = "http://localhost",
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                RoleClaimType = ClaimTypes.Role
            };
            options.Events = new JwtBearerEvents
            {
                OnChallenge = async context =>
                {
                    // Call this to skip the default logic and avoid using the default response
                    context.HandleResponse();

                    // Write to the response in any way you wish
                    context.Response.StatusCode = 401;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync("You are not authorized!");
                }
            };
        });

    // Configure authorization
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("AdminOnlyPolicy", policy =>
        {
            policy.RequireClaim(ClaimTypes.Role, "Admin");
        });
        options.AddPolicy("LegalOnlyPolicy", policy =>
        {
            policy.RequireClaim(ClaimTypes.Role, "Legal");
        });
    });

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        // Enable Swagger UI in development environment
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "AuthService API");
        });
    }
    // Omstil til Swagger UI, når man tilgår applikationens rod url
    app.Use(async (context, next) =>
                   {
                       if (context.Request.Path == "/")
                       {
                           context.Response.Redirect("/swagger/index.html");
                           return;
                       }

                       await next();
                   });
    app.UseHttpsRedirection();

    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    //If and errors ocurrs stop program 
    logger.Error(ex, "Stopped program because of exception");
    throw;
}
finally
{
    //Shutting Nlog down 
    NLog.LogManager.Shutdown();
}