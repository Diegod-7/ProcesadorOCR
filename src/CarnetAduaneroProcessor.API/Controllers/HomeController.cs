using Microsoft.AspNetCore.Mvc;

namespace CarnetAduaneroProcessor.API.Controllers
{
    /// <summary>
    /// Controlador para la página principal
    /// </summary>
    public class HomeController : Controller
    {
        /// <summary>
        /// Página principal de la aplicación
        /// </summary>
        [HttpGet("/")]
        public IActionResult Index()
        {
            return File("wwwroot/index.html", "text/html");
        }
    }
} 