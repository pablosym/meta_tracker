using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace Tracker.ViewComponents;

public class AppFooterViewComponent : ViewComponent
{
    private readonly IWebHostEnvironment _env;

    public AppFooterViewComponent(IWebHostEnvironment env)
    {
        _env = env;
    }

    public IViewComponentResult Invoke()
    {
        var environment = _env.EnvironmentName.ToUpperInvariant();

        var version = Assembly
            .GetExecutingAssembly()
            .GetName()
            .Version?
            .ToString(3);

        ViewBag.Environment = environment;
        ViewBag.Version = version;

        return View();
    }
}