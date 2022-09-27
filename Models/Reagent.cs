using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Feralas;

[Table("reagents")]
public class Reagent
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("item_id")]
    public int ItemId { get; set; }

    [Column("item_foreign_key")]
    public int ItemForeignKey { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; }
}