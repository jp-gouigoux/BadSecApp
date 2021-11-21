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
        public IActionResult Envoyer(string valeur)
        {
            // SECU (A03:2021-Injection) : Suppression du dossier en fonction des parametres - Risque de suppression d'un dossier non souhaité
            // Point de détail : si valeur contient / alors Path.Combine va se positionner sur la racine du disque et on va essayer de supprimer tout le disque dur
            // Dans le cas d'une suppression il faut veiller à bien comprendre la difference entre Path.Combine et Path.Join
            string dossier = Path.Join(RootPath, valeur);

            if (Directory.Exists(dossier))
            {
                Directory.Delete(dossier, true);
                return Ok();
            }

            return NotFound();
        }

        const string RootPath = "C:\\Apps\\Data";
    }
}