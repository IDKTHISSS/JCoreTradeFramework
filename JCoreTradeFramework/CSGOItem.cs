using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JCoreTradeFramework
{
    public class CSGOItem
    {
        public Int64 ID;
        public Int64 ClassID;
        public Int64 InstanceId;
        public bool IsTradeble;
        public string Name;
        public string Type;
        public string Rarity;
    }
    public enum SI_Type
    {
        Weapon,
        Spray,
        Case
    }
    public enum SI_Rarity
    {
        Common,
        Uncommon,
        Rare,
        Mythical,
        Legendary,
        Ancient,
        Exceedingly_Rare,
        Immortal
    }

}
