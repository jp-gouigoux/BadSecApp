using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
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
        private IConfiguration configuration;

        private CounterController(IConfiguration config)
        {
            configuration = config;
        }

        [HttpGet]
        public IActionResult Envoyer(string valeur)
        {
            string path = configuration["RootPath"];
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