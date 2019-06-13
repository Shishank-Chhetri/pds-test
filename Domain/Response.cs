using System;
using System.Collections.Generic;

namespace Domain
{
    public class CalanderEvents
    {
        public DateTime StartDate { get; }

        public DateTime EndDate { get; }

        public IEnumerable<Diary> DailyEvents { get; set; }
        
        public CalanderEvents(DateTime startDate, DateTime endDate, IEnumerable<Diary> logs)
        {
            StartDate = startDate;
            EndDate = endDate;
            DailyEvents = logs;
        }
    }

    public class Diary
    {
        public int Day { get; set; }

        public IEnumerable<Event> Events { get; set; }
    }

    public class Event
    {
        public int EventID { get; }

        public DateTime StartTime { get; }

        public DateTime EndTime { get; }

        public int SortOrder { get; set; }

        public String EventDescription { get; }

        public String Category { get; }

        public IEnumerable<int> Attendees { get; set; }

        public List<Members> AttendeeDetails { get; set; }

        public Event(int id, DateTime startTime, DateTime endTime, string description, string category, int sortOrder, IEnumerable<int> attendees)
        {
            EventID = id;
            StartTime = startTime;
            EndTime = endTime;
            EventDescription = description;
            Category = category;
            SortOrder = sortOrder;
            Attendees = attendees;

            AttendeeDetails = new List<Members>();
        }
    }

    public class Members
    {
        public int MemberID { get; }

        public string Name { get; }

        public string Constituency { get; }

        public string PartyAffiliation { get; set; }

        public Members(int id, string name, string constituency, string affiliation)
        {
            MemberID = id;
            this.Name = name;
            this.Constituency = constituency;
            PartyAffiliation = affiliation;
        }
    }
}
