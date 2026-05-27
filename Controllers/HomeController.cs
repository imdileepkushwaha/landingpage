using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SoftflipSolutions.Models;
using SoftflipSolutions.Data;
using SoftflipSolutions.Services;

namespace SoftflipSolutions.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ICaptchaService _captchaService;
    private readonly IPhoneValidationService _phoneValidationService;

    public HomeController(
        ApplicationDbContext context,
        ICaptchaService captchaService,
        IPhoneValidationService phoneValidationService)
    {
        _context = context;
        _captchaService = captchaService;
        _phoneValidationService = phoneValidationService;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public IActionResult RefreshCaptcha()
    {
        var challenge = _captchaService.GenerateChallenge();
        return Json(new { token = challenge.Token, question = challenge.Question });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitEnquiry(Enquiry enquiry, string captchaToken, string captchaAnswer)
    {
        if (!_captchaService.Validate(captchaToken, captchaAnswer))
        {
            TempData["ErrorMessage"] = "Captcha is incorrect or expired. Please try again.";
            return RedirectToAction(nameof(Index));
        }

        if (!_phoneValidationService.TryNormalize(enquiry.Phone, out var normalizedPhone))
        {
            TempData["ErrorMessage"] = "Please enter a valid phone number for the selected country.";
            return RedirectToAction(nameof(Index));
        }

        enquiry.Phone = normalizedPhone;
        enquiry.Message ??= string.Empty;
        enquiry.Status = "Pending";
        enquiry.CreatedAt = DateTime.Now;

        ModelState.Clear();
        if (!TryValidateModel(enquiry))
        {
            TempData["ErrorMessage"] = "Please fill all required fields correctly.";
            return RedirectToAction(nameof(Index));
        }

        _context.Enquiries.Add(enquiry);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Thank you for your enquiry. We will contact you soon.";
        TempData["SubmittedForm"] = "enquiry";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitDemoRequest(DemoRequest request, string captchaToken, string captchaAnswer, string formSource)
    {
        if (!_captchaService.Validate(captchaToken, captchaAnswer))
        {
            TempData["ErrorMessage"] = "Captcha is incorrect or expired. Please try again.";
            return RedirectToAction(nameof(Index));
        }

        if (!_phoneValidationService.TryNormalize(request.Phone, out var normalizedPhone))
        {
            TempData["ErrorMessage"] = "Please enter a valid phone number for the selected country.";
            return RedirectToAction(nameof(Index));
        }

        request.Phone = normalizedPhone;
        request.Message ??= string.Empty;
        request.Status = "Pending";
        request.CreatedAt = DateTime.Now;

        ModelState.Clear();
        if (!TryValidateModel(request))
        {
            TempData["ErrorMessage"] = "Please fill all required fields correctly.";
            return RedirectToAction(nameof(Index));
        }

        _context.DemoRequests.Add(request);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Demo request submitted successfully.";
        TempData["SubmittedForm"] = string.IsNullOrWhiteSpace(formSource) ? "demo-section" : formSource;
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
