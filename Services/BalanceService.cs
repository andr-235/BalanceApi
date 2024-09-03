using BalanceApi.Models;
using Microsoft.Extensions.FileProviders;
using Newtonsoft.Json;

namespace BalanceApi.Services
{
    public interface IBalanceService
    {
        Task<IEnumerable<BalanceSummary>> GetBalanceSummariesAsync(int accountId, string periodType);
        decimal CalculateCurrentDebt(int accountId);
    }

    public class BalanceService : IBalanceService
    {
        private readonly List<BalanceRecord> _balanceRecords;
        private readonly List<PaymentRecord> _paymentRecords;
        private readonly IFileProvider _fileProvider;
        private readonly ILogger<BalanceService> _logger;

        public BalanceService(IFileProvider fileProvider, ILogger<BalanceService> logger)
        {
            _fileProvider = fileProvider;
            _logger = logger;
            _logger.LogInformation("Initializing BalanceService");

            _balanceRecords = LoadBalanceRecords("balance_202105270825.json");
            _paymentRecords = LoadPaymentRecords("payment_202105270827.json");

            LogRecordLoadStatus(_balanceRecords, "balance");
            LogRecordLoadStatus(_paymentRecords, "payment");
        }

        public async Task<IEnumerable<BalanceSummary>> GetBalanceSummariesAsync(int accountId, string periodType)
        {
            _logger.LogInformation($"Fetching balances for AccountId: {accountId}, PeriodType: {periodType}");

            if (accountId <= 0 || string.IsNullOrEmpty(periodType))
            {
                _logger.LogWarning("Invalid parameters: AccountId: {AccountId}, PeriodType: {PeriodType}", accountId, periodType);
                return Enumerable.Empty<BalanceSummary>();
            }

            var filteredBalances = _balanceRecords.Where(r => r.AccountId == accountId).ToList();
            var filteredPayments = _paymentRecords.Where(p => p.AccountId == accountId).ToList();

            if (!filteredBalances.Any())
            {
                _logger.LogWarning($"No records found for AccountId: {accountId}");
                return Enumerable.Empty<BalanceSummary>();
            }

            var summaries = filteredBalances
                .GroupBy(r => GetPeriodGroup(r.Period.ToString(), periodType))
                .Select(group =>
                {
                    var periodName = group.Key;
                    var calculatedAmount = group.Sum(r => r.Calculation);
                    var paidAmount = filteredPayments
                        .Where(p => GetPeriodGroup(p.Date.ToString("yyyyMM"), periodType) == periodName)
                        .Sum(p => p.Sum);

                    var openingBalance = group.First().InBalance;
                    var closingBalance = openingBalance + calculatedAmount - paidAmount;

                    return new BalanceSummary
                    {
                        PeriodName = periodName,
                        OpeningBalance = openingBalance,
                        CalculatedAmount = calculatedAmount,
                        PaidAmount = paidAmount,
                        ClosingBalance = closingBalance
                    };
                })
                .OrderByDescending(s => s.PeriodName)
                .ToList();

            return await Task.FromResult(summaries);
        }

        public decimal CalculateCurrentDebt(int accountId)
        {
            var totalCalculation = _balanceRecords
                .Where(r => r.AccountId == accountId)
                .Sum(r => r.Calculation);

            var totalPaid = _paymentRecords
                .Where(p => p.AccountId == accountId)
                .Sum(p => p.Sum);

            return totalCalculation - totalPaid;
        }

        private string GetPeriodGroup(string period, string periodType)
        {
            return periodType switch
            {
                "year" => period.Substring(0, 4),
                "quarter" => $"{period.Substring(0, 4)}Q{(int.Parse(period.Substring(4, 2)) - 1) / 3 + 1}",
                "month" => period,
                _ => throw new ArgumentException("Invalid period type")
            };
        }

        private List<BalanceRecord> LoadBalanceRecords(string filePath)
        {
            _logger.LogInformation($"Loading balance records from file: {filePath}");
            var fileInfo = _fileProvider.GetFileInfo(filePath);

            if (!fileInfo.Exists)
            {
                _logger.LogError($"File not found: {filePath}");
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            using var stream = fileInfo.CreateReadStream();
            using var reader = new StreamReader(stream);
            var jsonContent = reader.ReadToEnd();

            try
            {
                var response = JsonConvert.DeserializeObject<BalanceResponse>(jsonContent);

                if (response == null || response.Balance == null)
                {
                    _logger.LogWarning($"Deserialized response or balance list is null for file: {filePath}");
                    return new List<BalanceRecord>();
                }

                return response.Balance;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, $"Error parsing JSON for balance file: {filePath}");
                return new List<BalanceRecord>();
            }
        }

        private List<PaymentRecord> LoadPaymentRecords(string filePath)
        {
            _logger.LogInformation($"Loading payment records from file: {filePath}");
            var fileInfo = _fileProvider.GetFileInfo(filePath);

            if (!fileInfo.Exists)
            {
                _logger.LogError($"File not found: {filePath}");
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            using var stream = fileInfo.CreateReadStream();
            using var reader = new StreamReader(stream);
            var jsonContent = reader.ReadToEnd();

            try
            {
                var records = JsonConvert.DeserializeObject<List<PaymentRecord>>(jsonContent);
                return records ?? new List<PaymentRecord>();
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, $"Error parsing JSON for payment file: {filePath}");
                return new List<PaymentRecord>();
            }
        }

        private void LogRecordLoadStatus<T>(List<T> records, string recordType)
        {
            if (records == null || !records.Any())
            {
                _logger.LogWarning($"No {recordType} records loaded.");
            }
            else
            {
                _logger.LogInformation($"{records.Count} {recordType} records loaded.");
            }
        }
    }
}
