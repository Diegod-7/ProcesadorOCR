using Microsoft.AspNetCore.Mvc;

namespace CarnetAduaneroProcessor.API.Controllers
{
    /// <summary>
    /// Controlador para la página principal
    /// </summary>
    public class HomeController : Controller
    {
        /// <summary>
        /// Página principal de la aplicación - Redirige a Swagger
        /// </summary>
        [HttpGet("/")]
        public IActionResult Index()
        {
            return Redirect("/swagger");
        }
    }
} 