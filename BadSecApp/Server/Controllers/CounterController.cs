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
            // [ACE/LNU] A01:2021-Broken Access Control:
            // [ACE/LNU] les répertoires par défaut C:\apps\data\1,2...x sont supprimés et on peut mettre tout chemin du disque...

            string dossier = Path.Combine(path, valeur);

            if (Directory.Exists(dossier))
            {
                Directory.Delete(dossier, true);
                return Ok();
            }

            return NotFound();
        }
    }
}