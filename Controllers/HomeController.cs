using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SoftflipSolutions.Models;
using SoftflipSolutions.Data;

namespace SoftflipSolutions.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> SubmitEnquiry(Enquiry enquiry)
    {
        if (ModelState.IsValid)
        {
            _context.Enquiries.Add(enquiry);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Thank you for your enquiry. We will contact you soon.";
            return RedirectToAction(nameof(Index));
        }
        TempData["ErrorMessage"] = "Please fill all required fields correctly.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> SubmitDemoRequest(DemoRequest request)
    {
        if (ModelState.IsValid)
        {
            _context.DemoRequests.Add(request);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Demo request submitted successfully.";
            return RedirectToAction(nameof(Index));
        }
        TempData["ErrorMessage"] = "Please fill all required fields correctly.";
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
