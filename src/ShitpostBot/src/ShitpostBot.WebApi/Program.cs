using FastEndpoints;
using ShitpostBot.Application;
using ShitpostBot.Application.Features.Repost;
using ShitpostBot.Infrastructure;
using ShitpostBot.Infrastructure.Services;
using ShitpostBot.WebApi.Services;

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

builder.Services.AddFastEndpoints();

var app = builder.Build();

app.MapGet("/", () => "ShitpostBot WebApi - Test Harness");

app.UseFastEndpoints(c =>
{
    c.Endpoints.Configurator = ep =>
    {
        ep.AllowAnonymous();
    };
});

app.Run();