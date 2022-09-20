using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Feralas
{
    [Table("skill_tiers")]
    public class SkillTier
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        [Column("name")]
        public string Name { get; set; }
        [Column("profession_id")]
        public int ProfessionId { get; set; }
    }
}

