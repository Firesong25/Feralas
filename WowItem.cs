using System.ComponentModel.DataAnnotations;


namespace Feralas
{
    public partial class WowItem
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

        public bool Equals(WowItem other)
        {
            if (other is null)
                return false;

            return Id == other.Id;
        }

        public override bool Equals(object obj) => Equals(obj as WowItem);
        public override int GetHashCode() => Id.GetHashCode();

    }
}

