using System;
using System.Globalization;

namespace TripleDBot.Model
{
    [Serializable]
    public sealed class Session : IEquatable<Session>
    {
        public string id { get; set; }
        public DateTimeOffset time { get; set; }
        public string location { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public string speaker { get; set; }

        public string speakerId { get; set; }

        public override string ToString()
        {
            return $"\"{this.title}\" by {this.speaker} at {this.time.ToLocalTime().ToString("hh:mm tt", CultureInfo.InvariantCulture)} in {this.location}";
        }

        public bool Equals(Session other)
        {
            return other != null
                && this.time == other.time
                && this.location == other.location;
        }

        public override bool Equals(object other)
        {
            return Equals(other as Session);
        }

        public override int GetHashCode()
        {
            return this.title.GetHashCode();
        }
    }
}