using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace TCPMaahomServer2
{
    class ServerHelper
    {

        static HttpClient client = new HttpClient();

        const string pass = "21VUSORmXuovFb";

        public static async Task SendData(string deviceId, string lat, string lng, string speed,string time)
        {
            string url = "https://portal.maahom.com/DeviceRequest/SetDataV2"
                + "?pw=" + pass
                + "&deviceId=" + deviceId
                + "&lat=" + lat
                + "&lng=" + lng
                + "&speed=" + speed
                + "&time=" + time;

            var result = await client.GetStringAsync(url);
        }

        public static async Task SendReading(string deviceId, string name,string hexvalue, string reading, string time)
        {
            string url = "https://portal.maahom.com/DeviceRequest/SetReading"
                + "?pw=" + pass
                + "&deviceId=" + deviceId
                + "&name=" + name
                + "&hexvalue=" + hexvalue
                + "&reading=" + reading
                + "&time=" + time;

            var result = await client.GetStringAsync(url);
        }

        public static async Task SendHeartBeat(string deviceId)
        {
            string url = "https://portal.maahom.com/DeviceRequest/SetHeartBeat"
                + "?pw=" + pass
                + "&deviceId=" + deviceId;

            var result = await client.GetStringAsync(url);
        }

        public static async Task SendEvent(string deviceId, string eventType, string time)
        {
            string url = "https://portal.maahom.com/DeviceRequest/SetEvent"
                + "?pw=" + pass
                + "&deviceId=" + deviceId
                + "&eventType=" + eventType
                + "&time=" + time;

            var result = await client.GetStringAsync(url);
        }
    }
}
