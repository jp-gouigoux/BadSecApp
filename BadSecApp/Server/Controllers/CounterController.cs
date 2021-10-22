using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Linq;
using System.Threading.Tasks;
using System.Security;
using System.Text;

namespace BadSecApp.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CounterController : Controller
    {
        private readonly IWebHostEnvironment _env;

        public CounterController(IWebHostEnvironment env)
        {
            _env = env;
        }

        [HttpGet]
        public IActionResult Envoyer(string valeur) // A08:2021-Software and Data Integrity Failures => Remove path setting : Don't allow to remove something outside from website
        {
            // Add defensive code to disallow parent folder traversal
            if (valeur.Contains(".."))
                throw new ArgumentException();

            if (HttpContext.Session.Get("USER") == null)
                throw new SecurityException();

            string dossier = Path.Combine(_env.ContentRootPath, "Users", Encoding.UTF8.GetString(HttpContext.Session.Get("USER")), valeur); // Restrict deletion only in user folder {website root}\Users\{login}\{valeur}

            if (Directory.Exists(dossier))
            {
                Directory.Delete(dossier, true);
                return Ok();
            }

            return NotFound();
        }
    }
}