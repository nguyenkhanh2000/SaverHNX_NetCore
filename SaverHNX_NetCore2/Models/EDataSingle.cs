using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace SaverHNX_NetCore2.Models
{
    public class EDataSingle:EBase
    {
        /// <summary>
        /// thoi gian update data vao redis (auto)
        /// </summary>
        [JsonProperty(Order = 1)]
        [DataMember(Name = __FULL_TIME, Order = 1)]
        public string Time { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

        /// <summary>
        /// noi dung data chinh can luu redis
        /// </summary>
        [JsonProperty(Order = 2)]
        [DataMember(Name = __FULL_DATA, Order = 2)]
        public List<IG_BI_FULL> Data { get; set; }

        /// <summary>
        /// constructor
        /// </summary>
        public EDataSingle(List<IG_BI_FULL> data) : base()
        {
            Data = data;
        }
    }
}
