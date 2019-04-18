namespace Gab.Shared.Models
{
    public class Event
    {
        public string Id { get; set; }
        public string Subject { get; set; }
        public string BodyPreview { get; set; }
        public string Start { get; set; }
        public string End { get; set; }
        public string Organizer { get; set; }
    }

    public class CreateEvent
    {
        public MeetingRoom MeetingRoom { get; set; }
        public string TimeZone { get; set; }
    }
}
