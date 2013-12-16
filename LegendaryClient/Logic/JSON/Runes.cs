using LegendaryClient.Logic.SQLite;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;
using System.Windows.Media.Imaging;

namespace LegendaryClient.Logic.JSON
{
    public static class Runes
    {
        public static List<runes> PopulateRunes()
        {
            List<runes> RuneList = new List<runes>();

            string runeJSON = File.ReadAllText(Path.Combine(Client.ExecutingDirectory, "Assets", "data", "en_US", "rune.json"));
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            Dictionary<string, object> deserializedJSON = serializer.Deserialize<Dictionary<string, object>>(runeJSON);
            Dictionary<string, object> runeData = deserializedJSON["data"] as Dictionary<string, object>;
            //Dictionary<string, object> treeData = deserializedJSON["tree"] as Dictionary<string, object>;

            foreach (KeyValuePair<string, object> rune in runeData)
            {
                runes newRune = new runes();
                Dictionary<string, object> singularRuneData = rune.Value as Dictionary<string, object>;
                newRune.id = Convert.ToInt32(rune.Key);
                newRune.name = singularRuneData["name"] as string;
                newRune.description = singularRuneData["description"] as string;
                newRune.stats = singularRuneData["stats"] as Dictionary<string, object>;
                newRune.tags = singularRuneData["tags"] as string[];

                Dictionary<string, object> imageData = singularRuneData["image"] as Dictionary<string, object>;
                var uriSource = Path.Combine(Client.ExecutingDirectory, "Assets", "rune", (string)imageData["full"]);
                newRune.icon = Client.GetImage(uriSource);

                RuneList.Add(newRune);
            }
            /*
            int i = 0;
            foreach (KeyValuePair<string, object> tree in treeData)
            {
                ArrayList list = (ArrayList)tree.Value;
                for (i = 0; i < list.Count; i++)
                {
                    foreach (Dictionary<string, object> x in (ArrayList)list[i])
                    {
                        if (x != null)
                        {
                            int MasteryId = Convert.ToInt32(x["masteryId"]);
                            masteries tempMastery = RuneList.Find(y => y.id == MasteryId);
                            tempMastery.treeRow = i;
                            tempMastery.tree = tree.Key;
                        }
                    }
                }
            }
            */
            return RuneList;
        }
    }
}