using Microsoft.AspNetCore.Mvc;
using SoftflipSolutions.Models;
using SoftflipSolutions.Services;

namespace SoftflipSolutions.ViewComponents;

public class CaptchaFieldViewComponent : ViewComponent
{
    private readonly ICaptchaService _captchaService;

    public CaptchaFieldViewComponent(ICaptchaService captchaService)
    {
        _captchaService = captchaService;
    }

    public IViewComponentResult Invoke(string prefix = "")
    {
        return View(new CaptchaFieldViewModel
        {
            Prefix = prefix,
            Challenge = _captchaService.GenerateChallenge()
        });
    }
}
