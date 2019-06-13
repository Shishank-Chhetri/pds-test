using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace EventCalanderRequest
{
    public static class ParliamentApi
    {
        public static HttpClient client = new HttpClient();

        const string eventApiUrl = "http://service.calendar.parliament.uk/calendar/events/list.xml";
        const string memberApiUrl = "http://data.parliament.uk/membersdataplatform/services/mnis/members/query/";

        //shared function to request xml from the api
        private static String RequestData(string url, string queryParams="")
        {
            var responseStr = "";
            HttpResponseMessage response = client.GetAsync(url + "/" + queryParams).Result;
            if (response.IsSuccessStatusCode)
            {
                responseStr = response.Content.ReadAsStringAsync().Result;
            } else
            {
                Console.WriteLine("{0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
            }
            return responseStr;
        }

        private static IEnumerable<T> ParseData<T>(string url, string queryParam, string NodeName, Func<XmlNode, T> parseFunction)
        {
            string response = RequestData(url, queryParam);

            IEnumerable<T> dataList;
            XmlDocument xmlDoc = new XmlDocument();
            try
            {
                xmlDoc.LoadXml(response);
                dataList = xmlDoc.SelectNodes(NodeName)
                                .Cast<XmlNode>()
                                .Select<XmlNode, T>(xmlElm => {
                                    return parseFunction(xmlElm);
                                    })
                                .Where<T>(nodeObj => nodeObj != null);
            }
            catch (XmlException ex) //there is error in the response, log and return empty array
            {
                dataList = new List<T>(0);
                Console.WriteLine("Error parsing response xml {0}", ex.InnerException);
            }

            return dataList;
        }

        public static Domain.CalanderEvents GetCalanderEvents(DateTime StartDate, DateTime EndDate)
        {
            //todo - caching on members cos that's not gonna change much. also on past events.

            string eventApiParam = string.Format("?House=Commons&Type=MainChamber&startdate={0}&enddate={1}", StartDate.ToString("yyyy-MM-dd"), EndDate.ToString("yyyy-MM-dd"));
            var eventList = ParseData(eventApiUrl, eventApiParam, "/ArrayOfEvent/Event", ParseEventXml).ToList();

            //there's bad request/ asp.net error when requesting lot of member ids at the same time
            var uniqueMemberID = eventList.SelectMany(evnt => evnt.Attendees).Distinct();
            List<Domain.Members> memberList = new List<Domain.Members>();
            while (uniqueMemberID.Count() > 0)
            {
                var membersPerRequest = 30;
                string memberApiParam = string.Format("house*Commons|id={0}", string.Join(",", uniqueMemberID.Take(membersPerRequest)));
                uniqueMemberID = uniqueMemberID.Skip(membersPerRequest).ToList();
                memberList.AddRange(ParseData(memberApiUrl, memberApiParam, "/Members/Member", ParseMemberXml));
            }

            //add attendee details to events - request less member info
            foreach(Domain.Event evnt in eventList)
            {
                foreach (int attendeeID in evnt.Attendees)
                {
                    evnt.AttendeeDetails.Add(memberList.FirstOrDefault(member => member.MemberID == attendeeID));
                }
            }


            //create daily events list by grouping the events by day
            IEnumerable<Domain.Diary> diaries = eventList.GroupBy(evnt => evnt.StartTime.ToShortDateString()).Select(group => new Domain.Diary() { Day = group.First().StartTime.Date.Day, Events = group.AsEnumerable() });

            return new Domain.CalanderEvents(StartDate, EndDate, diaries);
        }

        //monthly calander event
        public static Domain.CalanderEvents GetCalanderEvents(DateTime dateTime)
        {
            return GetCalanderEvents(dateTime, dateTime.AddMonths(1));
        }

        //function to create event object from event node
        private static Domain.Event ParseEventXml(XmlNode eventNodes)
        {
            int.TryParse(SafeGetValue(eventNodes.SelectSingleNode("@Id")), out int eventID);
            int.TryParse(SafeGetValue(eventNodes.SelectSingleNode("SortOrder")), out int SortOrder);
            string eventDescription = SafeGetValue(eventNodes.SelectSingleNode("Description")).Trim();
            string eventCategory = SafeGetValue(eventNodes.SelectSingleNode("Category")).Trim();

            DateTime.TryParse(SafeGetValue(eventNodes.SelectSingleNode("StartDate")), out DateTime eventStartDate);
            DateTime.TryParse(SafeGetValue(eventNodes.SelectSingleNode("EndDate")), out DateTime eventEndDate);

            TimeSpan.TryParse(SafeGetValue(eventNodes.SelectSingleNode("StartTime")), out TimeSpan startTime);
            TimeSpan.TryParse(SafeGetValue(eventNodes.SelectSingleNode("EndTime")), out TimeSpan endTime);

            eventStartDate = eventStartDate.Date + startTime;
            eventEndDate = eventEndDate + endTime;

            var eventAttendees = eventNodes.SelectNodes("Members/Member/@Id").Cast<XmlNode>().Select(attr => {
                bool result = int.TryParse(SafeGetValue(attr), out int value);
                return new { value, result };
            }).Where(result => result.result).Select(result => result.value);

            if(eventID > 0 & eventStartDate.Date != DateTime.MinValue && eventEndDate.Date != DateTime.MinValue)
            {
                return new Domain.Event(eventID, eventStartDate, eventEndDate, eventDescription, eventCategory, SortOrder, eventAttendees);
            }
            else
            {
                return null;
            }
        }

        //function to create member object from member node
        private static Domain.Members ParseMemberXml(XmlNode memberNode)
        {
            int.TryParse(SafeGetValue(memberNode.SelectSingleNode("@Member_Id")), out int memberID);
            string memberName = SafeGetValue(memberNode.SelectSingleNode("FullTitle")).Trim();
            string memberAffilation = SafeGetValue(memberNode.SelectSingleNode("Party")).Trim();
            string memberConstituency = SafeGetValue(memberNode.SelectSingleNode("MemberFrom")).Trim();

            return new Domain.Members(memberID, memberName, memberConstituency, memberAffilation);
        }

        private static string SafeGetValue(XmlNode xmlNode)
        {
            return (xmlNode is null) ? "" : xmlNode.Value ?? xmlNode.InnerText ?? "";
        }
    }
}
