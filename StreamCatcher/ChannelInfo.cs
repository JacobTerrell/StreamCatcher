using Newtonsoft.Json;

namespace StreamCatcher
{
    public class ChannelInfo
    {
        [JsonProperty("online")]
        public bool Online { get; set; }
    }
}
