using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Feralas
{
    [Table("professions")]
    public class Profession
    {
        [Column("id")]
        public int Id { get; set; }
        [Column("name")] 
        public string Name { get; set; }
    }
}

