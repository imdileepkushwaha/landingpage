using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoftflipSolutions.Data;
using SoftflipSolutions.Models;

namespace SoftflipSolutions.Controllers;

[Authorize(AuthenticationSchemes = "AdminCookie")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;

    public AdminController(ApplicationDbContext context)
    {
        _context = context;
    }

    [AllowAnonymous]
    public IActionResult Login()
    {
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            return RedirectToAction(nameof(Index));
        }
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Login(string username, string password)
    {
        var admin = await _context.AdminUsers.FirstOrDefaultAsync(u => u.Username == username && u.PasswordHash == password);
        
        if (admin != null)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, admin.Username)
            };

            var claimsIdentity = new ClaimsIdentity(claims, "AdminCookie");

            await HttpContext.SignInAsync("AdminCookie", new ClaimsPrincipal(claimsIdentity));

            return RedirectToAction(nameof(Index));
        }

        ViewBag.Error = "Invalid username or password";
        return View();
    }

    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("AdminCookie");
        return RedirectToAction(nameof(Login));
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.TotalEnquiries = await _context.Enquiries.CountAsync();
        ViewBag.TotalDemoRequests = await _context.DemoRequests.CountAsync();
        return View();
    }

    public async Task<IActionResult> Enquiries()
    {
        var enquiries = await _context.Enquiries.OrderByDescending(e => e.CreatedAt).ToListAsync();
        return View(enquiries);
    }

    public async Task<IActionResult> DemoRequests()
    {
        var requests = await _context.DemoRequests.OrderByDescending(e => e.CreatedAt).ToListAsync();
        return View(requests);
    }
}
