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

        public void CompareLists(List<WowAuction> incoming, List<WowAuction> stored, string tag)
        {
            LogMaker.Log($"There are {incoming.Count} new listings and {stored.Count} stored locally.");
            Stopwatch sw = Stopwatch.StartNew();

            List<WowAuction> freshListings = incoming.Except(stored).ToList();
            LogMaker.Log($"Linq says there are {freshListings.Count()} new auctions for {tag}. {sw.ElapsedMilliseconds} ms.");
            sw.Restart();

            
            var joined = (from b in stored
                         join bl in incoming on
                         new { AuctionId = b.AuctionId } equals
                         new { AuctionId = bl.AuctionId }
                         select b).ToList();

            int a = joined.Count;

            LogMaker.Log($"Linq says there are {a} to update for {tag}. {sw.ElapsedMilliseconds} ms.");
            sw.Restart();

            IEnumerable<WowAuction> sold = stored.Except(incoming).ToList();

            LogMaker.Log($"Linq says there are {sold.Count()} sold auctions for {tag}. {sw.ElapsedMilliseconds} ms.");
            sw.Restart();

            List<WowAuction> soldListings = new List<WowAuction>();

            List<WowAuction> updateableListings = new();

            int z = 0;
            foreach (WowAuction listing in incoming)
            {
                WowAuction trial = stored.FirstOrDefault(l => l.AuctionId == listing.AuctionId);
                if (trial == null)
                {
                    soldListings.Add(trial);    
                }
                else
                {
                    updateableListings.Add(trial);
                }
                z++;
                if (z % 10000 == 0)
                {
                    LogMaker.Log($"{z} processed.");
                }
            }

            a = soldListings.Count;
            LogMaker.Log($"Foreach says there are {soldListings.Count} sold and {updateableListings.Count} to update for {tag}. {sw.ElapsedMilliseconds} ms.");
            return;

        }

        public void FindDupeAuctionIds(List<WowAuction> longList)
        {
            List<WowAuction> dupes = new();

            int a = 0;
            foreach (WowAuction w in longList)
            {
                var trialAuctions = longList.Where(l => l.AuctionId == w.AuctionId && l.ItemId != w.ItemId).ToList();
                if (trialAuctions.Count > 1)
                {
                    dupes.Add(w); a++;
                }
            }

            a = dupes.Count;
        }

        public async Task<List<WowAuction>> GetSoldItemsAsync(List<WowAuction> incoming, List<WowAuction> stored, string tag)
        {
            await Task.Delay(1);
            List<WowAuction> soldListings = new List<WowAuction>();
            return soldListings;
        }
    }
}
