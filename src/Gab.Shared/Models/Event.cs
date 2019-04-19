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
        public string TimeZone { get; set; }
    }

    public class CreateEvent
    {
        public MeetingRoom MeetingRoom { get; set; }
        public string TimeZone { get; set; }
    }

    public static class EventMappings
    {
        public static Event ToEvent(this Microsoft.Graph.Event e)
        {
            return new Event
            {
                Id = e.Id,
                Subject = e.Subject,
                BodyPreview = e.BodyPreview,
                Start = e.Start.DateTime,
                End = e.End.DateTime,
                Organizer = e.Organizer.EmailAddress.Name,
                TimeZone = e.OriginalStartTimeZone           
            };
        }
    }
}
