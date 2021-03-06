﻿using System;
using System.Globalization;
using Newtonsoft.Json;
using PropertyChanged;

namespace Gab.Shared.Models
{
    [AddINotifyPropertyChangedInterface]
    public class Event
    {
        #region Properties

        public string Id { get; set; }
        public string Subject { get; set; }
        public string BodyPreview { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string Organizer { get; set; }
        public string TimeZone { get; set; }
        public ChangeType ChangeType { get; set; } = ChangeType.None;
        public string MeetingRoom { get; set; }
        [JsonIgnore]
        public bool IsCurrent { get; set; }

        #endregion

        #region Methods

        public Event Update(Event e)
        {
            Start = e.Start;
            End = e.End;
            BodyPreview = e.BodyPreview;
            ChangeType = e.ChangeType;
            Organizer = e.Organizer;
            Subject = e.Subject;
            TimeZone = e.TimeZone;
            MeetingRoom = e.MeetingRoom;

            return this;
        }

        public Event ConvertTimeToTimeZone(string timeZone)
        {
            Start = TimeZoneInfo.ConvertTimeFromUtc(Start, TimeZoneInfo.FindSystemTimeZoneById(timeZone));
            End = TimeZoneInfo.ConvertTimeFromUtc(End, TimeZoneInfo.FindSystemTimeZoneById(timeZone));
            return this;
        }

        #region Equals

        public override bool Equals(object obj) => Equals(obj as Event);

        public bool Equals(Event other)
        {
            return other != null &&
                   //other.Id == Id &&
                   other.Start == Start &&
                   other.End == End &&
                   other.Organizer == Organizer &&
                   other.Subject == Subject &&
                   other.BodyPreview == BodyPreview &&
                   other.MeetingRoom == MeetingRoom &&
                   other.TimeZone == TimeZone &&
                   other.ChangeType == ChangeType;
        }

        public override int GetHashCode() => Id.GetHashCode();

        #endregion

        #region Operators

        public static bool operator ==(Event ev1, Event ev2)
        {
            if (ReferenceEquals(ev1, ev2))
                return true;

            if (ev1 is null)
                return false;

            if (ev2 is null)
                return false;

            return ev1.Equals(ev2);
        }

        // this is second one '!='
        public static bool operator !=(Event ev1, Event ev2) => !(ev1 == ev2);

        #endregion 

        #endregion 
    }

    public class CreateEvent
    {
        public MeetingRoom MeetingRoom { get; set; }
        public string TimeZone { get; set; }
    }

    public class EndsEvent
    {
        public string Id { get; set; }
        public string Ended { get; set; }
        public MeetingRoom MeetingRoom { get; set; }
        public string TimeZone { get; set; }
    }

    public static class EventMappings
    {
        public static Event ToEvent(this Microsoft.Graph.Event e, ChangeType changeType = ChangeType.None, string meetingRoom = null)
        {
            var ev = new Event
            {
                Id = e.Id,
                Subject = e.Subject,
                BodyPreview = e.BodyPreview,
                Start = DateTime.Parse(e.Start.DateTime, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal).ToUniversalTime(),
                End = DateTime.Parse(e.End.DateTime, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal).ToUniversalTime(),
                Organizer = e.Organizer.EmailAddress.Name,
                TimeZone = e.OriginalStartTimeZone,
                ChangeType = changeType,
                MeetingRoom = meetingRoom
            };
            return ev;
        }
    }

    public enum ChangeType
    {
        None = 0,
        Created = 1,
        Updated = 2,
        Deleted = 3
    }
}
