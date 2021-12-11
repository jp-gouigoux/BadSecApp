using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
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
        private readonly ILogger<AuthenticationController> _logger;

        private IConfiguration _config;

        public AuthenticationController(ILogger<AuthenticationController> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

        [HttpGet]
        public StatusCodeResult Login([FromQuery] string login, [FromQuery] string pwd)
        {
            if (login is null) throw new ArgumentException("login cannot be empty");
            if (pwd is null) throw new ArgumentException("password cannot be empty");

            bool isAuthenticated = false;
            try
            {
                var salt = _config.GetValue<string>("sel-pour-mots-de-passe"); // Dans une approche simple, on passe le sel "Zorglub64" en variable d'environnement, mais on pourrait durcir bien sûr encore plus
                var content = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(string.Concat(salt,pwd)));
                StringBuilder sb = new StringBuilder();
                foreach (byte b in content)
                    sb.Append(b.ToString("x2"));
                string hash = sb.ToString().ToLower();

                if (login == "admin" && hash == "b00d11bc27e970579921c0775b7c15599224b606bb2a582bf94952c956b5a679")
                    isAuthenticated = true;
                else if (login != "admin")
                {
                    using (var conn = new SqliteConnection("Data Source=test.db"))
                    {
                        conn.Open();
                        var commande = conn.CreateCommand();
                        commande.CommandText = "SELECT hash FROM USERS WHERE login=@login";
                        commande.Parameters.Add(new SqliteParameter("login", login));
                        if (commande.ExecuteScalar()?.ToString() == hash)
                            isAuthenticated = true;
                    }
                }
            }
            catch (Exception excep)
            {
                _logger.LogDebug(excep.ToString());
            }

            if (isAuthenticated)
            {
                HttpContext.Session.Set("USER", Encoding.UTF8.GetBytes(login));
                return new OkResult();
            }
            else
            {
                _logger.LogWarning("Tentative d'authentification incorrecte sur le login" + login);
                return new UnauthorizedResult();
            }
        }

        [HttpGet("validate")]
        public StatusCodeResult ValidateUsersList(string xmlContent)
        {
            // SECU (A08:2021-Software and Data Integrity Failures) : potentielle attaque par XML bombing, si on laisse du contenu entrer tel quel et qu'on ne met pas de validation ou de limite sur les ressources (mais pas facile à blinder)
            XmlDocument dom = new XmlDocument();
            dom.LoadXml(xmlContent);
            if (dom.SelectNodes("//users").Count > 0)
                return new OkResult();
            else
                return NotFound();
        }
    }
}
