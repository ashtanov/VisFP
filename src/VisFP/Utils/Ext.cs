using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static VisFP.Models.RegularGrammar;

namespace VisFP.Utils
{
    public static class Ext
    {
        public static RgNode AddOrGetRgNode(this Dictionary<char, RgNode> dict, char nT)
        {
            RgNode node;
            if (!dict.TryGetValue(nT, out node))
            {
                node = new RgNode(nT);
                dict.Add(nT, node);
            }
            else
                node = dict[nT];
            return node;
        }

        public static List<string> DeserializeJsonListOfStrings(this string str)
        {
            return JsonConvert.DeserializeObject<List<string>>(str);
        }

        public static string SerializeJsonListOfStrings(this List<string> list)
        {
            return JsonConvert.SerializeObject(list);
        }
    }
}
