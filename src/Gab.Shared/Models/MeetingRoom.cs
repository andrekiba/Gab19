namespace Gab.Shared.Models
{
    public class MeetingRoom
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Mail { get; set; }
        public string Color { get; set; }

        #region Equals

        public override bool Equals(object obj) => Equals(obj as MeetingRoom);

        public bool Equals(MeetingRoom other)
        {
            return other != null && other.Id == Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        #region Operators

        public static bool operator ==(MeetingRoom mr1, MeetingRoom mr2)
        {
            if (ReferenceEquals(mr1, mr2))
                return true;

            if (mr1 is null || mr2 is null)
                return false;

            return mr1.Equals(mr2);
        }

        // this is second one '!='
        public static bool operator !=(MeetingRoom mr1, MeetingRoom mr2) => !(mr1 == mr2);

        #endregion 

        #endregion 
    }
}
