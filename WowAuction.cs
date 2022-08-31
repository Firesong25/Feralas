using System.ComponentModel.DataAnnotations;


namespace Feralas
{
    public class WowAuction
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

            return this.Id == other.Id;
        }

        public override bool Equals(object obj) => Equals(obj as WowAuction);
        public override int GetHashCode() => (Id).GetHashCode();
    }
   
}

