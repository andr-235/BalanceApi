using BalanceApi.Models;
using BalanceApi.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace BalanceApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BalanceController : ControllerBase
    {
        private readonly IBalanceService _balanceService;
        private readonly ILogger<BalanceController> _logger;

        public BalanceController(IBalanceService balanceService, ILogger<BalanceController> logger)
        {
            _balanceService = balanceService;
            _logger = logger;
        }

        [HttpGet("GetBalances")]
        public async Task<IActionResult> GetBalances(int accountId, string periodType)
        {
            try
            {
                if (accountId <= 0)
                {
                    return BadRequest("Invalid account ID.");
                }

                var result = await _balanceService.GetBalanceSummariesAsync(accountId, periodType);

                if (result == null || !result.Any())
                {
                    return NotFound("No balance summaries found.");
                }

                var acceptHeader = Request.Headers["Accept"].ToString();

                if (acceptHeader.Contains("application/xml"))
                {
                    return Ok(result); 
                }
                else if (acceptHeader.Contains("text/csv"))
                {
                    var csv = ConvertToCsv(result);
                    return File(Encoding.UTF8.GetBytes(csv), "text/csv", "balances.csv");
                }

                return Ok(result); 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching balances.");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("GetCurrentDebt")]
        public IActionResult GetCurrentDebt(int accountId)
        {
            try
            {
                if (accountId <= 0)
                {
                    return BadRequest("Invalid account ID.");
                }

                var debt = _balanceService.CalculateCurrentDebt(accountId);
                return Ok(new { CurrentDebt = debt });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while calculating current debt.");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        private string ConvertToCsv(IEnumerable<BalanceSummary> summaries)
        {
            var csv = new StringBuilder();
            csv.AppendLine("PeriodName,OpeningBalance,CalculatedAmount,PaidAmount,ClosingBalance");

            foreach (var summary in summaries)
            {
                csv.AppendLine($"{summary.PeriodName},{summary.OpeningBalance},{summary.CalculatedAmount},{summary.PaidAmount},{summary.ClosingBalance}");
            }

            return csv.ToString();
        }
    }
}
