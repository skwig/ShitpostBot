using System.Diagnostics;

namespace ShitpostBot.Infrastructure;

public static class ShitpostBotActivitySource
{
    public const string Name = "ShitpostBot";

    public static readonly ActivitySource Instance = new(Name);
}
