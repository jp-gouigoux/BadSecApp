using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Unicode;
using System.Threading.Tasks;
using System.Xml;

namespace BadSecApp.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthenticationController : Controller
    {
    // [ACE/LNU] A07:2021-Identification and Authentication Failures | A02:2021-Cryptographic Failures:
    // [ACE/LNU] Problème classique de Basic authentication over http, il est possible d'intercepter le login/pwd de la query (vu avec Fiddler)
    // [ACE/LNU] De plus, le password est en clair...

    private readonly ILogger<AuthenticationController> _logger;

        public AuthenticationController(ILogger<AuthenticationController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public StatusCodeResult Login([FromQuery] string login, [FromQuery] string pwd)
        {
            if (login is null) throw new ArgumentException("login cannot be empty");
            if (pwd is null) pwd = string.Empty;

            // [ACE/LNU] A04:2021-Insecure Design ou A08:2021-Software and Data Integrity Failures:
            // [ACE/LNU] Avec une initialisation à TRUE pour isAuthenticated, il suffit de faire crasher le bloc try ci-après pour être authentifié
            // [ACE/LNU] Faire crasher l'ordre par injection par exemple en fermant la chaîne + ajout de n'importe quelle clause...
            bool isAuthenticated = true;
            try
            {
                var content = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(pwd));
                StringBuilder sb = new StringBuilder();
                foreach (byte b in content)
                    sb.Append(b.ToString("x2"));
                string hash = sb.ToString().ToLower();

                // [ACE/LNU] A06:2021-Vulnerable and Outdated Components:
                // [ACE/LNU] Encryptage MD5 accessible par toute appli de déchiffrement, ici Hash correspond à 'superman', potentiellement attaquable par force bruce
                if (login == "admin" && hash != "84d961568a65073a3bcf0eb216b2a576")
                    isAuthenticated = false;
                else if (login != "admin")
                {
                    using (var conn = new SqliteConnection("Data Source=test.db"))
                    {
                        conn.Open();
                        var commande = conn.CreateCommand();

                        // [ACE/LNU] A03:2021-Injection:
                        // [ACE/LNU] Pas de sanitisation en entrée sur le login, injection SQL possible
                        commande.CommandText = "SELECT hash FROM USERS WHERE login='" + login + "'";
                        if (commande.ExecuteScalar()?.ToString() != hash)
                            isAuthenticated = false;
                    }
                }
            }
            catch (Exception excep)
            {
                // [ACE/LNU] A09: 2021 - Security Logging and Monitoring Failures:
                // [ACE/LNU] Niveau de traces pas suffisant pour tracer les attaques, il faudrait afficher le login/password donnés par le hacker
                _logger.LogDebug(excep.ToString());
            }

            if (isAuthenticated)
            {
                HttpContext.Session.Set("USER", Encoding.UTF8.GetBytes(login));
                return new OkResult();
            }
            else
            {
                return new UnauthorizedResult();
            }
        }

        [HttpGet("validate")]
        public StatusCodeResult ValidateUsersList(string xmlContent)
        {
            XmlDocument dom = new XmlDocument();
            dom.LoadXml(xmlContent);
            if (dom.SelectNodes("//users").Count > 0)
                return new OkResult();
            else
                return NotFound();
        }
    }
}
