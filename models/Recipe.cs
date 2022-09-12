using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Feralas
{
    [Table("recipes")]

    public class Recipe : IEquatable<Recipe>
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        [Column("name")]
        public string Name { get; set; }
        [Column("profession_id")]
        public int ProfessionId { get; set; }

        [Column("skill_tier_id")]
        public int SkillTierId { get; set; }


        [Column("category")]
        public string Category { get; set; }
        public bool Equals(Recipe other)
        {
            if (other is null)
                return false;

            return Id == other.Id;
        }

        public override bool Equals(object obj) => Equals(obj as Recipe);
        public override int GetHashCode() => Id.GetHashCode();

    }
}
