using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Feralas
{
    [Table("wow_auctions")]
    public class WowAuction : IEquatable<WowAuction>
    {
        [Column("id")]
        [Key]
        public int Id { get; set; }
        //public string PartitionKey { get; set; }
        [Column("connected_realm_id")]
        public int ConnectedRealmId { get; set; }
        [Column("auction_id")]
        public int AuctionId { get; set; }
        [Column("first_seen_time")]
        public DateTime FirstSeenTime { get; set; }
        [Column("last_seen_time")]
        public DateTime LastSeenTime { get; set; }
        [Column("time_left")]
        [DefaultValue(TimeLeft.VERY_LONG)]
        public TimeLeft TimeLeft { get; set; }
        [Column("sold")]
        public bool Sold { get; set; }
        [Column("quantity")]
        public int Quantity { get; set; }
        [Column("item_id")]
        public int ItemId { get; set; }
        [Column("unit_price")]
        public long UnitPrice { get; set; }
        [Column("buyout")]
        [DefaultValue(0)]
        public long Buyout { get; set; }

        public bool Equals(WowAuction other)
        {
            if (other is null)
                return false;

            return AuctionId == other.AuctionId && ItemId == other.ItemId;
        }

        public override bool Equals(object obj) => Equals(obj as WowAuction);
        public override int GetHashCode() => (AuctionId, ItemId).GetHashCode();
    }

    public enum TimeLeft
    {
        VERY_LONG,
        LONG,
        MEDIUM,
        SHORT
    }

}

