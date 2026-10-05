using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShitpostBot.Application.Features.About;
using ShitpostBot.Application.Features.DailySlop;
using ShitpostBot.Application.Features.DailySlop.Detectors;
using ShitpostBot.Application.Features.DeletedMessages;
using ShitpostBot.Application.Features.EditedMessages;
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
            services.AddMessageFeature<TrackDeletedMessagesFeature>();
            services.AddMessageFeature<DeletedCommand>();

            services.AddSingleton<EditedMessageStore>();
            services.AddMessageFeature<TrackEditedMessagesFeature>();
            services.AddMessageFeature<EditedCommand>();

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

            services.AddMessageFeature<DailySlopLeaderboardCommand>();
            services.AddMessageFeature<DailySlopCommand>();
            services.AddMessageFeature<DailySlopFeature>();
            services.AddSingleton<IDailySlopDetector, TravleDetector>();
            services.AddSingleton<IDailySlopDetector, GlobleDetector>();
            services.AddSingleton<IDailySlopDetector, MaptapDetector>();
            services.AddSingleton<IDailySlopDetector, CutleDetector>();
            services.AddSingleton<IDailySlopDetector>(new FoodguessrDetector("foodguessr", false));
            services.AddSingleton<IDailySlopDetector>(
                new FoodguessrDetector("foodguessr-plateoff", true)
            );
            services.AddSingleton<IDailySlopDetector, KindahardGolfDetector>();
            services.AddSingleton<IDailySlopDetector, ScrandleDetector>();
            services.AddSingleton<RngdleDetector>();
            services.AddSingleton<IDailySlopDetector>(sp =>
                sp.GetRequiredService<RngdleDetector>()
            );
            services.AddSingleton<IDailySlopScoringStrategy>(sp =>
                sp.GetRequiredService<RngdleDetector>()
            );
            foreach (
                var detector in new[]
                {
                    new RunedleDetector("runedle", "Runedle"),
                    new RunedleDetector("runedle-expert", "Runedle (expert)"),
                }
            )
            {
                services.AddSingleton<IDailySlopDetector>(detector);
                services.AddSingleton<IDailySlopScoringStrategy>(detector);
            }
            services.AddSingleton<IDailySlopDetector>(
                new SizeItUpDetector("size-it-up", "/size-it-up")
            );
            services.AddSingleton<IDailySlopDetector>(
                new SizeItUpDetector("size-it-up-geography", "/size-it-up/geography")
            );
            services.AddSingleton<IDailySlopDetector>(
                new SizeItUpDetector("size-it-up-pop-culture", "/size-it-up/pop-culture")
            );

            services.AddMessageFeature<ImageRepostFeature>();
            services.AddMessageFeature<LinkRepostFeature>();
            services.AddMessageFeature<SusFeature>();

            // Must be registered last as a fallback
            services.AddMessageFeature<UnknownCommand>();

            return services;
        }
    }
}
