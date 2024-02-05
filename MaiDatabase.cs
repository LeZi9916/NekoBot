using System;
using System.Collections.Generic;
using System.Linq;
using static TelegramBot.MaiScanner;
using static TelegramBot.Config;
using System.Threading.Tasks;
using System.Threading;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;

namespace TelegramBot
{
    internal static class MaiDatabase
    {
        internal static List<MaiAccount> MaiAccountList = new();
        internal static List<int> MaiInvaildUserIdList = new();
        internal static List<long> RatingList = new();
        internal static List<IGrouping<long, MaiAccount>> Top = new();

        internal static void MaiDataInit()
        {
            foreach (var user in TUserList)
                user.GetMaiAccountInfo();
            CalRating();
        }
        internal static void CalRating()
        {
            var allRating = MaiAccountList.OrderBy(x => x.playerRating);
            RatingList = allRating.OrderByDescending(x => x.playerRating).Select(x =>x.playerRating).ToList();
            var top = allRating.Skip(allRating.Count() - 300).OrderByDescending(x => x.playerRating); 
            var ratingGroup = top.GroupBy(x => x.playerRating);

            Top = ratingGroup.ToList();
        }
        internal static async Task<long> GetUserRank(long rating)
        {
            return await Task.Run(() => 
            {
                var rankList = RatingList.GroupBy(x => x);
                int ranking = 1;
                foreach (var rankGroup in rankList)
                {
                    if (rankGroup.Key == rating)
                        return ranking;
                    ranking += rankGroup.Count();
                }
                return -1;
            });
        }
    }
}
