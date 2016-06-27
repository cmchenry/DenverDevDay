using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TripleDBot.Model
{
    public class SessionQuery
    {
        public IList<string> Topics { get; set; }

        public string Speaker { get; set; }

        public DateTimeOffset StartTime { get; set; }

        public DateTimeOffset EndTime { get; set; }

        public string Room { get; set; }

    }
}