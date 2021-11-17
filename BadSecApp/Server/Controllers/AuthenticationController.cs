using BadSecApp.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Xml;

namespace BadSecApp.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthenticationController : Controller
    {
        private readonly ILogger<AuthenticationController> _logger;

        public AuthenticationController(ILogger<AuthenticationController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        public StatusCodeResult Login([FromBody] Credentials credentials)
        {
            if (credentials is null) throw new ArgumentException("login cannot be empty");

            bool isAuthenticated = false;

            try
            {
                var content = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(credentials.pwd));
                StringBuilder sb = new StringBuilder();
                foreach (byte b in content)
                    sb.Append(b.ToString("x2"));
                string hash = sb.ToString().ToLower();

                if (credentials.login == "admin" && hash == "84d961568a65073a3bcf0eb216b2a576") // On pourrait utiliser SHA512
                {
                    isAuthenticated = true;
                }
                else if (credentials.login != "admin")
                {
                    using (var conn = new SqliteConnection("Data Source=test.db"))
                    {
                        conn.Open();
                        var commande = conn.CreateCommand();
                        commande.CommandText = "SELECT hash FROM USERS WHERE login='" + credentials.login + "'";
                        if (commande.ExecuteScalar()?.ToString() == hash)
                        {
                            isAuthenticated = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex.ToString());
            }

            if (isAuthenticated)
            {
                HttpContext.Session.Set("USER", Encoding.UTF8.GetBytes(credentials.login));
                return new OkResult();
            }
            else
            {
                _logger.LogError("Echec d'authentification de l'utilisateur " + credentials.login);
                return new UnauthorizedResult();
            }
        }


        // On devrait changer la méthode pour POST et annoter avec [RequestSizeLimit]
        [HttpGet("validate")]
        public StatusCodeResult ValidateUsersList(string xmlContent)
        {
            // Il faudrait valider le XML contre un XSD, limiter la taille de la chaîne aussi. Ou tout simplement passer sur du JSON.
            XmlDocument dom = new XmlDocument();
            dom.LoadXml(xmlContent);
            if (dom.SelectNodes("//users").Count > 0)
                return new OkResult();
            else
                return NotFound();
        }
    }
}
