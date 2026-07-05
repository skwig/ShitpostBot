using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShitpostBot.Application.Features.About;
using ShitpostBot.Application.Features.DailySlop;
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
            services.AddMessageFeature<StatsCommand>();
            services.AddMessageFeature<RepostMatchCommand>();
            services.AddMessageFeature<RepostMatchAllCommand>();
            services.AddMessageFeature<RepostWhitelistCommand>();
            services.AddMessageFeature<RepostUnwhitelistCommand>();
            services.AddMessageFeature<SearchCommand>();
            services.AddMessageFeature<NineteenEightyFourCommand>();
            services.AddMessageFeature<SugmaBallsCommand>();
            services.AddMessageFeature<WumpusCommand>();
            services.AddMessageFeature<HelpCommand>();
            services.AddMessageFeature<UnknownCommand>();

            services.AddMessageFeature<DailySlopCommand>();

            services.AddMessageFeature<DailySlopFeature>();

            services.AddScoped<IDailySlopDetector, TravleDetector>();
            services.AddScoped<IDailySlopDetector, GlobleDetector>();
            services.AddScoped<IDailySlopDetector, MaptapDetector>();
            services.AddScoped<IDailySlopDetector, CutleDetector>();
            services.AddScoped<IDailySlopDetector, FoodguessrDetector>();
            services.AddScoped<IDailySlopDetector, PlateOffDetector>();
            services.AddScoped<IDailySlopDetector, KindahardGolfDetector>();
            services.AddScoped<IDailySlopDetector, ScrandleDetector>();

            services.AddMessageFeature<ImageRepostFeature>();
            services.AddMessageFeature<LinkRepostFeature>();
            services.AddMessageFeature<SusFeature>();

            return services;
        }
    }
}
