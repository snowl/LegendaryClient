using LegendaryClient.Logic.SQLite;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace LegendaryClient.Logic.JSON
{
    public static class Champions
    {
        public static void InsertExtraChampData(champions Champ)
        {
            string champJSON = File.ReadAllText(Path.Combine(Client.ExecutingDirectory, "Assets", "data", "en_US", "champion", Champ.name + ".json"));
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            Dictionary<string, object> deserializedJSON = serializer.Deserialize<Dictionary<string, object>>(champJSON);
            Dictionary<string, object> temp = deserializedJSON["data"] as Dictionary<string, object>;
            Dictionary<string, object> champData = temp[Champ.name] as Dictionary<string, object>;

            Champ.Lore = champData["lore"] as string;
            Champ.ResourceType = champData["partype"] as string;
            Champ.Skins = champData["skins"] as ArrayList;
            ArrayList Spells = (ArrayList)champData["spells"];
            Champ.Spells = new List<Spell>();

            foreach (Dictionary<string, object> champSpells in Spells)
            {
                Spell NewSpell = new Spell();
                NewSpell.ID = champSpells["id"] as string;
                NewSpell.Name = champSpells["name"] as string;
                NewSpell.Description = champSpells["description"] as string;
                NewSpell.Tooltip = champSpells["tooltip"] as string;
                NewSpell.MaxRank = champSpells["maxrank"] as string;



                Champ.Spells.Add(NewSpell);
            }
        }
    }
}
