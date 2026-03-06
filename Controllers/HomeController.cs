using Microsoft.AspNetCore.Mvc;
using Tracker.DTOs;
using Tracker.Models;

namespace Tracker.Controllers;

public class HomeController(IConfiguration configuration, Tracker_DevelContext context, IHttpContextAccessor contextAccessor) : BaseController(context, contextAccessor)
{


    private readonly IConfiguration _configuration = configuration;

    public PartialViewResult Monitor()
    {

        return PartialView("Monitor");

    }
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    /// <summary>
    /// Manejo de horrores
    /// </summary>
    /// <param name="statusCode"></param>
    /// <returns></returns>
    public IActionResult Error(int statusCode)
    {
        if (statusCode == 404)
        {
            return View("404");
        }
        else
        {
            return View("Error");
        }
    }

    [HttpGet]
    [IgnoreAntiforgeryToken]
    public IActionResult ManualDeUsuario()
    {

        // return Ok();
        try
        {

            var file = _configuration["ManualDeUsuario"];

            if (file == null || string.IsNullOrEmpty(file))
            {
                SetMessage(new MessageDTO() { Status= MessageDTO.Estatus.ERROR, Value ="No se encuentra en archivo" });
                return View(nameof(Index));
            }

            return File(file, "application/pdf", $"{DateTime.Now:yyyy-MM-dd}_ManualDeUsuario.pdf");
            


        }
        catch (Exception ex)
        {

            ModelState.AddModelError("", ex.Message);
            var messageDTO = new MessageDTO() { Status = MessageDTO.Estatus.ERROR, Value = "No se encuentro el pedido" };

            return Ok(messageDTO);
        }
    }
}