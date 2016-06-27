using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TripleDBot.Model
{
    [Serializable]
    public class Speaker
    {
        public string id { get; set; }
        public string name { get; set; }
        public string bio { get; set; }
        public string headshot { get; set; }
        public string blog { get; set; }
        public string twitter { get; set; }

        public override string ToString()
        {
            return string.Concat($"{name} ({id}): {bio}");
        }
    }
}