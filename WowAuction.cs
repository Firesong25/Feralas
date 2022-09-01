using System.ComponentModel.DataAnnotations;

/*
 https://docs.microsoft.com/en-us/dotnet/api/system.linq.enumerable.except?redirectedfrom=MSDN&view=net-6.0#System_Linq_Enumerable_Except__1_System_Collections_Generic_IEnumerable___0__System_Collections_Generic_IEnumerable___0__
 
 */

namespace Feralas
{
    public class WowAuction : IEquatable<WowAuction> //
    {

        public string PartitionKey { get; set; }
        public int AuctionId { get; set; }
        public DateTime FirstSeenTime { get; set; }
        public DateTime LastSeenTime { get; set; }        
        public bool ShortTimeLeftSeen { get; set; }
        public bool Sold { get; set; }
        public int Quantity {get; set;}
        public int ItemId { get; set; }
        public long UnitPrice { get; set; }
        public long? Buyout { get; set; }

        [Key]
        public Guid Id { get; set; }

        public bool Equals(WowAuction other)
        {
            if (other is null)
                return false;

            return AuctionId == other.AuctionId && ItemId == other.ItemId;
        }

        public override bool Equals(object obj) => Equals(obj as WowAuction);
        public override int GetHashCode() => (AuctionId, ItemId).GetHashCode();
    }
   
}

