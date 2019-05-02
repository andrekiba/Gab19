namespace Gab.Shared.Models
{
    public class MeetingRoom
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Mail { get; set; }
        public string Color { get; set; }

        public override bool Equals(object obj) => Equals(obj as MeetingRoom);

        public bool Equals(MeetingRoom other)
        {
            return other != null && other.Id == Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}
