using System;

namespace Gab.Shared.Models
{
    public class Event
    {
        public string Id { get; set; }
        public string Subject { get; set; }
        public string BodyPreview { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
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
                Start = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.Parse(e.Start.DateTime), e.OriginalStartTimeZone),
                End = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.Parse(e.End.DateTime), e.OriginalStartTimeZone),
                Organizer = e.Organizer.EmailAddress.Name,
                TimeZone = e.OriginalStartTimeZone           
            };
        }
    }
}
