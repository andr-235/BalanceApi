using Newtonsoft.Json;

namespace BalanceApi.Models
{
    public class PaymentRecord
    {
        [JsonProperty("account_id")]
        public int AccountId { get; set; }
        [JsonProperty("date")]
        public DateTime Date { get; set; }
        [JsonProperty("sum")]
        public decimal Sum { get; set; }
        [JsonProperty("payment_guid")]
        public Guid PaymentGuid { get; set; }
    }
}
