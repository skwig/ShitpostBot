using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShitpostBot.Application.Features.About;
using ShitpostBot.Application.Features.DailySlop;
using ShitpostBot.Application.Features.DeletedMessages;
using ShitpostBot.Application.Features.DailySlop.Detectors;
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

            services.AddSingleton<DeletedMessageStore>();
            services.AddMessageFeature<DeletedMessagesFeature>();

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

            services.AddMessageFeature<DailySlopCommand>();
            services.AddMessageFeature<UnknownCommand>();

            services.AddMessageFeature<DailySlopFeature>();

            services.AddSingleton<IDailySlopDetector, TravleDetector>();
            services.AddSingleton<IDailySlopDetector, GlobleDetector>();
            services.AddSingleton<IDailySlopDetector, MaptapDetector>();
            services.AddSingleton<IDailySlopDetector, CutleDetector>();
            services.AddSingleton<IDailySlopDetector, FoodguessrDetector>();
            services.AddSingleton<IDailySlopDetector, PlateOffDetector>();
            services.AddSingleton<IDailySlopDetector, KindahardGolfDetector>();
            services.AddSingleton<IDailySlopDetector, ScrandleDetector>();

            services.AddMessageFeature<ImageRepostFeature>();
            services.AddMessageFeature<LinkRepostFeature>();
            services.AddMessageFeature<SusFeature>();

            return services;
        }
    }
}
