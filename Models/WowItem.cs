using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
#nullable enable

namespace Feralas
{
    [Table("wow_items")]
    public partial class WowItem : IEquatable<WowItem>
    {
        [Column("item_id")]
        public int ItemId { get; set; }
        [Column("name")]
        public string Name { get; set; }
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

        public bool Equals(WowItem other)
        {
            if (other is null)
                return false;

            return Id == other.Id;
        }

        public override bool Equals(object obj) => Equals(obj as WowItem);
        public override int GetHashCode() => Id.GetHashCode();
    }

    public partial class OldItem
    {

        public int ItemId { get; set; }
        public string Name { get; set; }
        public string? BonusList { get; set; }
        public int? PetBreedId { get; set; }
        public int? PetLevel { get; set; }
        public int? PetQualityId { get; set; }
        public int? PetSpeciesId { get; set; }
        [Key]
        public Guid Id { get; set; }

    }
    }

