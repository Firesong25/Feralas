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
        public async Task CompareListsFromJson(string json, string tag)
        {
            
            Listings auctions = new();
            await auctions.CreateLists(json);
            List<WowAuction> incoming = auctions.LiveAuctions;
            PostgresContext context = new();
            List<WowAuction> stored = context.WowAuctions.ToList();
            CompareLists(incoming, stored, tag);

        }

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
    }
}
