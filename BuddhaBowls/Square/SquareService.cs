using System;
using System.Collections.Generic;
using Square.Connect.Api;
using Square.Connect.Client;
using Square.Connect.Model;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using System.Globalization;

namespace BuddhaBowls.Square
{
    public class SquareService
    {
        private const int GMT_ADJUST = -7;
        TransactionsApi _api;
        public SquareService()
        {
            Configuration.Default.AccessToken = Properties.Settings.Default.SquareToken;
            _api = new TransactionsApi();
        }

        // Retrieving your location IDs
        public void RetrieveLocations()
        {
            LocationsApi _locationsApi = new LocationsApi();
            var response = _locationsApi.ListLocations();
        }

        public List<SquareSale> ListTransactions(DateTime startTime, DateTime endTime)
        {
            List<SquareSale> sales = new List<SquareSale>();
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(string.Format("https://connect.squareup.com/v1/{0}/payments?begin_time={1}&end_time={2}",
                                                                   Properties.Settings.Default.SquareLocationId,
                                                                   WebUtility.UrlEncode(ToSquareDateFormat(startTime)),
                                                                   WebUtility.UrlEncode(ToSquareDateFormat(endTime))));
            req.Method = "GET";
            req.Headers["Authorization"] = "Bearer " + Properties.Settings.Default.SquareToken;
            WebResponse response;

            while (true)
            {
                response = req.GetResponse();
                Stream responseStream = response.GetResponseStream();
                string respString;
                using (StreamReader reader = new StreamReader(responseStream, Encoding.ASCII))
                {
                    respString = reader.ReadToEnd();
                }

                dynamic jsonResponse = JsonConvert.DeserializeObject(respString);
                foreach (dynamic resp in jsonResponse)
                {
                    SquareSale sale = new SquareSale(resp);
                    if(sale.GrossSales > 0)
                        sales.Add(sale);
                }

                if (response.Headers["Link"] == null)
                    break;
                req = (HttpWebRequest)WebRequest.Create(response.Headers["Link"].Split(new char[] { '<', '>' })[1]);
                req.Method = "GET";
                req.Headers["Authorization"] = "Bearer " + Properties.Settings.Default.SquareToken;
            } 

            return sales;
        }

        public static string ToSquareDateFormat(DateTime time)
        {
            return time.ToString(string.Format("yyyy-MM-ddTHH:mm:ss-0{0}:00", -(int)TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow).TotalHours));
        }

        public static DateTime FromSquareDateString(string time)
        {
            return DateTime.Parse(time).AddHours(TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow).TotalHours);
        }
    }
}
