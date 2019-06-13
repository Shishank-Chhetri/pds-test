using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace PDS.Models
{
    public class CalanderEventsController : ApiController
    {
        // GET api/CalanderEvents/5/2019
        [System.Web.Services.WebMethod]
        public IHttpActionResult GetCalanaderData(int monthNo, int year)
        {
            try
            {
                var calanderData = EventCalanderRequest.ParliamentApi.GetCalanderEvents(new DateTime(year, monthNo, 1), new DateTime(year, monthNo + 1, 1).AddDays(-1));
                return Json(System.Web.Helpers.Json.Encode(calanderData));
            }
            catch (Exception ex)
            {
                Console.WriteLine("error returning calander events. {0}", ex.InnerException);
                throw ex;
            }
        }

        [System.Web.Services.WebMethod]
        public IHttpActionResult GetCalanaderData()
        {
            return GetCalanaderData(DateTime.Now.Month, DateTime.Now.Year);
        }
    }
}