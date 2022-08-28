using System.ComponentModel.DataAnnotations;


namespace Feralas
{
    public partial class WowAuction
    {
        //public WowAuction()
        //{ }
        //public WowAuction(int auctionId, int quantity, int itemId, long unitPrice, long? buyout)
        //{
        //    AuctionId = auctionId;
        //    FirstSeenTime = DateTime.Now;
        //    LastSeenTime = DateTime.Now;
        //    Quantity = quantity;
        //    ItemId = itemId;
        //    UnitPrice = unitPrice;
        //    Buyout = buyout;
        //}

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

