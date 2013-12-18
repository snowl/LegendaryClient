using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegendaryClient.Logic.SQLite
{
    public class Spell
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Tooltip { get; set; }
        public string MaxRank { get; set; }
        public string CooldownBurn { get; set; }
        public string InitialCooldown { get; set; }
        public string CostBurn { get; set; }
        public string InitalCost { get; set; }
        public string Image { get; set; }
        public string Range { get; set; }
        public string Resource { get; set; }
    }
}
