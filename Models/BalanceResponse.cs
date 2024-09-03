using Newtonsoft.Json;

namespace BalanceApi.Models
{
    public class BalanceResponse
    {
        [JsonProperty("balance")]
        public List<BalanceRecord> Balance { get; set; }
    }
}
