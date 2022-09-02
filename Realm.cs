using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Feralas
{
    // https://us.api.blizzard.com/data/wow/realm/index?namespace=dynamic-us&locale=en_US&access_token=UScH0Q8jPENSvGlvCHSyI4piteZkmiWhBm
    //https://eu.api.blizzard.com/data/wow/realm/index?namespace=dynamic-eu&locale=en_US&access_token=UScH0Q8jPENSvGlvCHSyI4piteZkmiWhBm
    public class Realm
    {
        public string Name;
        public string WowNamespace;
        public int ConnectedRealmId;
        
    }
}
