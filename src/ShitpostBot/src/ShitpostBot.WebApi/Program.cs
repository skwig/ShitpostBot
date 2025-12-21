using ShitpostBot.Application;
using ShitpostBot.Application.Features.Repost;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Services;
using ShitpostBot.WebApi.Services;
using ShitpostBot.WebApi.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddShitpostBotInfrastructure(builder.Configuration);
builder.Services.AddShitpostBotApplication(builder.Configuration);
builder.Services.AddShitpostBotMassTransit(builder.Configuration, x =>
{
    x.AddConsumer<EvaluateRepost_ImagePostTrackedHandler>();
    x.AddConsumer<EvaluateRepost_LinkPostTrackedHandler>();
});
builder.Services.AddSingleton<IChatClient, NullChatClient>();
builder.Services.AddSingleton<TestMessageFactory>();
builder.Services.AddSingleton<IBotActionStore, BotActionStore>();

var app = builder.Build();

app.MapGet("/", () => "ShitpostBot WebApi - Test Harness");
app.MapTestEndpoints();

app.Run();