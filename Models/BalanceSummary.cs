namespace BalanceApi.Models
{
    public class BalanceSummary
    {
        public string PeriodName { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal CalculatedAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal ClosingBalance { get; set; }
    }
}
