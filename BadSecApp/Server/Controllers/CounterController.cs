using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BadSecApp.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CounterController : Controller
    {
        [HttpGet]
        public IActionResult Envoyer(string valeur, string path)
        {
            // SECU (A03:2021-Injection) : Suppression du dossier en fonction des parametres - Risque de suppression d'un dossier non souhaité
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