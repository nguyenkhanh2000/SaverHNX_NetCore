using SaverHNX_NetCore2.Settings;
using StackExchange.Redis;
using System.Net;
using System.Net.Http;

namespace SaverHNX_NetCore2.Extensions
{
    public class CTracker
    {
        private readonly string _apiUrl;
        private readonly string _appName;
        private readonly string _hostName;
        private Tracker_Setting _trackerSetting;
        private readonly HttpClient _httpClient;
        private readonly SemaphoreSlim semaphoreTelegraf = new SemaphoreSlim(1, 1);
        public CTracker(Tracker_Setting _setting)
        {
            try
            {
                this._trackerSetting = _setting;
                _apiUrl = "http://10.26.7.240:8086/write?db=ScheduleLog&u=fitfap&p=fitfap@123";
                //_apiUrl = _trackerSetting.API_URL;
                _appName = _trackerSetting.APPNAME_GRAFANA;
                _hostName = Dns.GetHostName();
                var proxy = new WebProxy
                {
                    Address = new Uri("http://10.26.2.55:8080"),
                };
                // Initialize HttpClient
                var handler = new HttpClientHandler
                {
                    //Proxy = proxy,
                    UseProxy = true
                };
                _httpClient = new HttpClient();
            }
            catch (Exception ex)
            {
                CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
            }
        }
        public async Task MsgRD2InfluxDBAsync(int msgcount)
        {
            await semaphoreTelegraf.WaitAsync();
            try
            {
                string payload = BuildMetricsPayload(msgcount, "MsgCount");

                // Send the payload asynchronously using HttpClient
                var content = new StringContent(payload);

                // Since InfluxDB usually expects plain text line protocol
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");

                HttpResponseMessage response = await _httpClient.PostAsync(_apiUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    CLog.LogError(CBase.GetDeepCaller(), $"Failed to send data to InfluxDB: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                CLog.LogError(CBase.GetDeepCaller(), CBase.GetDetailError(ex));
            }
            finally
            {
                semaphoreTelegraf.Release();
            }
        }

        private string BuildMetricsPayload(int count, string message)
        {
            DateTime currentTime = DateTime.UtcNow;
            long unixTimeMilliseconds = (long)(currentTime - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;

            // Construct the payload in InfluxDB Line Protocol format
            string payload = $"ScheduleTaskDaily,host={_hostName},app={_appName} count={count},message=\"{message}\" {unixTimeMilliseconds}000000";
            return payload;
        }

        // Dispose HttpClient when done
        //public void Dispose()
        //{
        //    _httpClient.Dispose();
        //}
    }
}
