using EquityLens.Api.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace EquityLens.Api.Controllers;

[ApiController]
[Route("api/methodology")]
public class MethodologyController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(MethodologyDto), StatusCodes.Status200OK)]
    public ActionResult<MethodologyDto> GetMethodology() =>
        Ok(new MethodologyDto(
            Summary: "EquityLens combines historical price behavior, financial trend stability, and recent headline signals into a transparent company research view.",
            RiskFormula: "Final Risk Score = Volatility*0.30 + MaxDrawdown*0.25 + RevenueInstability*0.15 + EarningsInstability*0.15 + DebtPressure*0.10 + NewsRisk*0.05",
            Components:
            [
                new MethodologyComponentDto("Volatility", 0.30m, "Annualized standard deviation of daily returns using roughly 252 trading days."),
                new MethodologyComponentDto("Max drawdown", 0.25m, "Largest peak-to-trough decline across the available historical price series."),
                new MethodologyComponentDto("Revenue instability", 0.15m, "Penalizes unstable or declining annual revenue trends."),
                new MethodologyComponentDto("Earnings instability", 0.15m, "Penalizes unstable or declining net income trends."),
                new MethodologyComponentDto("Debt pressure", 0.10m, "Uses debt-to-cash balance and liability growth as simple balance-sheet pressure signals."),
                new MethodologyComponentDto("News risk", 0.05m, "Uses keyword categories, recency, and source quality to estimate headline risk.")
            ]));
}
