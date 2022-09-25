using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Feralas;

[Table("margin_reports")]

public class MarginReport
{
    [Column("id")]
    [Key]
    public int Id { get; set; }

    [Column("connected_realm_id")]
    public int ConnectedRealmId { get; set; }

    [Column("item_id")]
    public int ItemId { get; set; }

    [Column("profession_id")]
    public int ProfessionId { get; set; }

    [Column("volume_48_hours")]
    public int Volume48Hours { get; set; }

    [Column("sales_price")]
    public long SalesPrice { get; set; }

    [Column("costs_price")]
    public long CostsPrice { get; set; }
    [NotMapped]
    public int SalesPriceGold
    {
        get
        {
            if (LongToGold(SalesPrice)[0] > 0)
            {
                return LongToGold(SalesPrice)[0];
            }
            return 0;

        }
    }
    [NotMapped]
    public int SalePriceSilver
    {
        get
        {
            if (LongToGold(SalesPrice)[1] > 0)
            {
                return LongToGold(SalesPrice)[1];
            }
            return 0;

        }
    }

    [NotMapped]
    public int SalesPriceCopper
    {
        get
        {
            if (LongToGold(SalesPrice)[2] > 0)
            {
                return LongToGold(SalesPrice)[2];
            }
            return 0;
        }
    }

    [NotMapped]
    public int CostPriceGold
    {
        get
        {
            if (LongToGold(CostsPrice)[0] > 0)
            {
                return LongToGold(CostsPrice)[0];
            }
            return 0;
        }
    }

    [NotMapped]
    public int CostPriceSilver
    {
        get
        {
            if (LongToGold(CostsPrice)[1] > 0)
            {
                return LongToGold(CostsPrice)[1];
            }
            return 0;
        }
    }

    [NotMapped]
    public int CostPriceCopper
    {
        get
        {
            if (LongToGold(CostsPrice)[2] > 0)
            {
                return LongToGold(CostsPrice)[2];
            }
            return 0;
        }
    }


    static int[] LongToGold(long money)
    {
        int[] result = new int[3];
        int gold = (int)money / 10000;
        result[0] = gold;
        money = money % 10000;
        int silver = (int)money / 1000;
        result[1] = silver;
        int copper = (int)money % 1000;
        result[2] = copper;
        return result;
    }
}
