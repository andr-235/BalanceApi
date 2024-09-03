using Newtonsoft.Json;

namespace BalanceApi.Models
{
    public class BalanceRecord
    {
        [JsonProperty("account_id")]
        public int AccountId { get; set; }

        [JsonProperty("period")]
        public int Period { get; set; }

        [JsonProperty("in_balance")]
        public decimal InBalance { get; set; }

        [JsonProperty("calculation")]
        public decimal Calculation { get; set; }
    }
}
