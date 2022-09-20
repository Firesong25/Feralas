using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
#nullable enable

namespace Feralas
{
    [Table("wow_items")]
    public class WowItem
    {
        [Column("item_id")]
        public int ItemId { get; set; }
        [Column("name")]
        public string? Name { get; set; }
        [Column("bonus_list")]
        public string? BonusList { get; set; }
        [Column("pet_breed_id")]
        public int? PetBreedId { get; set; }
        [Column("pet_level")]
        public int? PetLevel { get; set; }
        [Column("pet_quality_id")]
        public int? PetQualityId { get; set; }
        [Column("pet_species_id")]
        public int? PetSpeciesId { get; set; }
        [Column("id")]
        [Key]
        public Guid Id { get; set; }
    }
}

