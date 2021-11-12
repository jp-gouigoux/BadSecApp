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
            // A03 - Injection de type Path Traversal
            // Vérifier que le début du contenu commence bien par le nom canonique (= nom le plus simple possible de ce que l'on veut). Si non renvoyer une exception.
            // Le chemin saisit en paramètre ne doit pas contenir ".."
            // Sécuriser les sous dossier, principe du moindre privilège
            // Utiliser un serveur web qui utilise un compte avec très peu de droits
            // Utiliser la sécurité intégrée Windows, à condition d'avoir un AD
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