using System.ComponentModel.DataAnnotations;

namespace Feralas
{
    public class WowRealm
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string WowNamespace { get; set; }
        public int? ConnectedRealmId { get; set; }
    }

}
