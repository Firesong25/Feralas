using System.ComponentModel.DataAnnotations;


namespace Feralas
{
    public partial class WowAuction
    {
        [Key]
        public long PrimaryKey { get; set; }

        public int ConnectedRealmId { get; set; }
        public int AuctionId { get; set; }
        public DateTime FirstSeenTime { get; set; }
        public DateTime LastSeenTime { get; set; }
        public bool ShortTimeLeftSeen { get; set; }
        public bool Sold { get; set; }
        public int Quantity {get; set;}
        public int ItemId { get; set; }
        public long UnitPrice { get; set; }
        public long? Buyout { get; set; }
    }
}

