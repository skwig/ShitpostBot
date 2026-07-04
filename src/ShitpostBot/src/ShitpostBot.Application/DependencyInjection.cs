using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShitpostBot.Application.Features.About;
using ShitpostBot.Application.Features.Help;
using ShitpostBot.Application.Features.NineteenEightyFour;
using ShitpostBot.Application.Features.Repost;
using ShitpostBot.Application.Features.Search;
using ShitpostBot.Application.Features.Stats;
using ShitpostBot.Application.Features.SugmaBalls;
using ShitpostBot.Application.Features.Sus;
using ShitpostBot.Application.Features.Unknown;
using ShitpostBot.Application.Features.Wumpus;
using ShitpostBot.Application.MessageRouting;

namespace ShitpostBot.Application;

public static class DependencyInjection
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddShitpostBotApplication(IConfiguration configuration)
        {
            services.AddSingleton<MessageRouter>();

            services.AddMessageFeature<AboutCommand>();
            services.AddMessageFeature<StatsFeature>();
            services.AddMessageFeature<RepostMatchFeature>();
            services.AddMessageFeature<RepostMatchAllFeature>();
            services.AddMessageFeature<RepostWhitelistFeature>();
            services.AddMessageFeature<RepostUnwhitelistFeature>();
            services.AddMessageFeature<SearchFeature>();
            services.AddMessageFeature<NineteenEightyFourFeature>();
            services.AddMessageFeature<SugmaBallsFeature>();
            services.AddMessageFeature<WumpusFeature>();
            services.AddMessageFeature<HelpFeature>();
            services.AddMessageFeature<UnknownFeature>();

            services.AddMessageFeature<ImageRepostFeature>();
            services.AddMessageFeature<LinkRepostFeature>();
            services.AddMessageFeature<SusFeature>();

            return services;
        }
    }
}