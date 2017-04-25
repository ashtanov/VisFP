using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VisFP.BusinessObjects
{
    [JsonObject]
    public struct PetryFlowLink
    {
        public string from;
        public string to;
    }

    [JsonObject]
    public class PetryNet
    {
        public readonly string[] P;
        public readonly string[] T;
        public readonly PetryFlowLink[] F;
        public PetryNet(string[] P, string[] T, PetryFlowLink[] F)
        {
            this.P = new string[P.Length];
            P.CopyTo(this.P, 0);
            this.T = new string[T.Length];
            T.CopyTo(this.T, 0);
            this.F = new PetryFlowLink[F.Length];
            F.CopyTo(this.F, 0);
        }
        public string Serialize()
        {
            return JsonConvert.SerializeObject(this);
        }

        public static PetryNet Deserialize(string json)
        {
            return JsonConvert.DeserializeObject<PetryNet>(json);
        }
    }


}
