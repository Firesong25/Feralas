using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Feralas
{
    [Table("crafted_items")]
    public class CraftedItem : IEquatable<CraftedItem>
    {
        [Column("id")]
        [Key]
        public int Id { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("profession_id")]
        public int ProfessionId { get; set; }
        [Column("skill_tier_id")]
        public int SkillTierId { get; set; }
        [Column("category")]
        public string Category { get; set; }
        [Column("reagents")]
        public string Reagents { get; set; }
        [Column("minimum_quantity")]
        public int MinumumQuantity { get; set; }
        [Column("maximum_quantity")]
        public int MaximumQuantity { get; set; }

        [Column("recipe_id")]
        public int RecipeId { get; set; }

        public bool Equals(CraftedItem other)
        {
            if (other is null)
                return false;

            return Id == other.Id;
        }

        public override bool Equals(object obj) => Equals(obj as Recipe);
        public override int GetHashCode() => Id.GetHashCode();
    }
}
