using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace BadSecApp.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CounterController : Controller
    {
        // Devrait être soumis à [Authorize], etre renommé et re-conçu.
        [HttpGet]
        public IActionResult Envoyer(string valeur, string path)
        {
            string dossier = Path.Join(path, valeur);

            if (Directory.Exists(dossier))
            {
                Directory.Delete(dossier, true);
                return Ok();
            }

            return NotFound();
        }
    }
}