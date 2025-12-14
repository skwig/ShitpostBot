using ShitpostBot.Application;
using ShitpostBot.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddShitpostBotInfrastructure(builder.Configuration);
builder.Services.AddShitpostBotApplication(builder.Configuration);
builder.Services.AddShitpostBotMassTransit(builder.Configuration, x =>
{
    // WebApi doesn't consume messages initially, only publishes
});

var app = builder.Build();

app.MapGet("/", () => "ShitpostBot WebApi - Test Harness");

app.Run();
