using ShitpostBot.Application;
using ShitpostBot.Infrastructure;
using ShitpostBot.WebApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddShitpostBotInfrastructure(builder.Configuration);
builder.Services.AddShitpostBotApplication(builder.Configuration);
builder.Services.AddShitpostBotMassTransit(builder.Configuration, x =>
{
    // WebApi doesn't consume messages initially, only publishes
});
builder.Services.AddSingleton<IChatClient, NullChatClient>();

var app = builder.Build();

app.MapGet("/", () => "ShitpostBot WebApi - Test Harness");

app.Run();
