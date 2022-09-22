using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Feralas
{
    internal class Intersections
    {
        // https://stackoverflow.com/questions/37389027/use-linq-to-get-items-in-one-list-that-are-in-another-list
        //public async Task CompareListsFromJson(string json, string tag)
        //{
            
        //    Listings auctions = new();
        //    await auctions.CreateLists(json, tag);
        //    List<WowAuction> incoming = auctions.LiveAuctions;
        //    PostgresContext context = new();
        //    List<WowAuction> stored = context.WowAuctions.ToList();
        //    CompareLists(incoming, stored, tag);

        //}

        public void CompareLists(List<WowAuction> incoming, List<WowAuction> stored, string tag)
        {
            Stopwatch sw = Stopwatch.StartNew();

            List<WowAuction> freshListings = incoming.Except(stored).ToList();
            LogMaker.Log($"Linq says there are {freshListings.Count()} new auctions for {tag}. {sw.ElapsedMilliseconds} ms.");
            sw.Restart();

            List<WowAuction> updatableListings = (from b in stored
                                                  join bl in incoming on
                                                  new { AuctionId = b.AuctionId } equals
                                                  new { AuctionId = bl.AuctionId }
                                                  select b).ToList();

            LogMaker.Log($"Linq query says there are {updatableListings.Count} to update for {tag}. {sw.ElapsedMilliseconds} ms.");
            sw.Restart();


            var result = incoming.Intersect(stored).ToList();

            LogMaker.Log($"Linq Intersect says there are {result.Count} to update for {tag}. {sw.ElapsedMilliseconds} ms.");
            sw.Restart();


            List<WowAuction> sold = stored.Except(incoming).ToList();
            LogMaker.Log($"Linq says there are {sold.Count()} sold auctions for {tag}. {sw.ElapsedMilliseconds} ms.");
            sw.Restart();

        }

        public List<WowAuction> FindDupeAuctionIds(List<WowAuction> longList)
        {
            List<WowAuction> dupes = new();

            int a = 0;
            foreach (WowAuction w in longList)
            {
                var trialAuctions = longList.Where(l => l.AuctionId == w.AuctionId).ToList();
                if (trialAuctions.Count > 1)
                {
                    dupes.Add(w); a++;
                }
            }

            return dupes;
        }

        public async Task<List<WowAuction>> GetSoldItemsAsync(List<WowAuction> incoming, List<WowAuction> stored, string tag)
        {
            await Task.Delay(1);
            List<WowAuction> soldListings = new List<WowAuction>();
            return soldListings;
        }

        public void CreateMatsDictionary()
        {
            Dictionary<int, int> tradeItemsCosts = new();
            tradeItemsCosts.Add(180733, 90000);
            tradeItemsCosts.Add(178787, 1250000);
            tradeItemsCosts.Add(20815, 800);
            tradeItemsCosts.Add(6217, 124);
            tradeItemsCosts.Add(187812, 2500000);
            tradeItemsCosts.Add(3371, 400);
            tradeItemsCosts.Add(183950, 90000);
            tradeItemsCosts.Add(172056, 5000);
            tradeItemsCosts.Add(172057, 3750);
            tradeItemsCosts.Add(172058, 4500);
            tradeItemsCosts.Add(172059, 4250);
            tradeItemsCosts.Add(178786, 3500);
            tradeItemsCosts.Add(159, 5);
            tradeItemsCosts.Add(30817, 5);
            tradeItemsCosts.Add(2687, 2);
            tradeItemsCosts.Add(177062, 110000);
            tradeItemsCosts.Add(183955, 90000);
            tradeItemsCosts.Add(177061, 5000);
            tradeItemsCosts.Add(173060, 1000);
            tradeItemsCosts.Add(180732, 500);
            tradeItemsCosts.Add(175886, 1000);
            tradeItemsCosts.Add(183953, 90000);
            tradeItemsCosts.Add(39489, 5000);
            tradeItemsCosts.Add(39505, 750);
            tradeItemsCosts.Add(183952, 90000);
            tradeItemsCosts.Add(173168, 10000);
            tradeItemsCosts.Add(183954, 90000);
        }
    }
}
