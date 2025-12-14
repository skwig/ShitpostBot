using ShitpostBot.Application;
using ShitpostBot.Infrastructure;
using ShitpostBot.WebApi.Services;
using ShitpostBot.WebApi.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddShitpostBotInfrastructure(builder.Configuration);
builder.Services.AddShitpostBotApplication(builder.Configuration);
builder.Services.AddShitpostBotMassTransit(builder.Configuration, x =>
{
    // WebApi doesn't consume messages initially, only publishes
});
builder.Services.AddSingleton<IChatClient, NullChatClient>();
builder.Services.AddSingleton<TestMessageFactory>();

var app = builder.Build();

app.MapGet("/", () => "ShitpostBot WebApi - Test Harness");
app.MapTestEndpoints();

app.Run();
