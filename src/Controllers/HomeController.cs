using FlociDashboard.Services;
using Microsoft.AspNetCore.Mvc;

namespace FlociDashboard.Controllers;

public class HomeController(FlociService floci, RegionService regionService, ILogger<HomeController> logger) : Controller
{
    public async Task<IActionResult> Index()
    {
        try
        {
            var summary = await floci.GetDashboardSummaryAsync();
            ViewBag.FlociEndpoint = floci.FlociEndpoint;
            ViewBag.CurrentRegion = regionService.CurrentRegion;
            ViewBag.AvailableRegions = regionService.AvailableRegions;
            return View(summary);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load dashboard");
            ViewBag.Error = ex.Message;
            ViewBag.FlociEndpoint = floci.FlociEndpoint;
            ViewBag.CurrentRegion = regionService.CurrentRegion;
            ViewBag.AvailableRegions = regionService.AvailableRegions;
            return View(new FlociDashboard.Models.DashboardSummary());
        }
    }

    [HttpPost]
    public IActionResult SetRegion(string region, string? returnUrl)
    {
        regionService.CurrentRegion = region;
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        return RedirectToAction(nameof(Index));
    }
}
