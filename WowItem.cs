using System.ComponentModel.DataAnnotations;

#nullable disable

namespace Feralas
{
    public partial class WowItem
    {
        [Key]

        public int PrimaryKey { get; set; }
        public int ItemId { get; set; }        
        public string Name { get; set; }
        public string BonusList { get; set; }
        public int? PetBreedId { get; set; }
        public int? PetLevel { get; set; }
        public int? PetQualityId { get; set; }
        public int? PetSpeciesId { get; set; }

    }
}

