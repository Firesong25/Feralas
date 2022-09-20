using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Feralas
{
    [Table("wow_realms")]
    public class WowRealm
    {
        [Column("id")]
        [Key]
        public int Id { get; set; }
        [Column("name")]
        public string Name { get; set; } = null!;
        [Column("realm_slug")]
        public string RealmSlug { get; set; }
        [Column("wow_namespace")]
        public string WowNamespace { get; set; } = null!;
        [Column("connected_realm_id")]
        [DefaultValue(0)]
        public int ConnectedRealmId { get; set; }
        [Column("activley_scanning")]
        public bool IsActivelyScanning { get; set; }
        [Column("last_scan_time")]
        public DateTime LastScanTime { get; set; }
        [Column("scan_report")]
        public string ScanReport { get; set; }
    }
}
