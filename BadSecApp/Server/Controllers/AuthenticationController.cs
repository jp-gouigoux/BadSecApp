using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
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

        public AuthenticationController(ILogger<AuthenticationController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public StatusCodeResult Login([FromQuery] string login, [FromQuery] string pwd)
        {
            if (login is null) throw new ArgumentException("login cannot be empty");
            if (pwd is null) pwd = string.Empty;

            bool isAuthenticated = false; // SECU (A01:2021-Broken Access Control) : on doit travailler en failsafe, le booléen devrait être initialisé à false et passé à true que si la preuve d'authentification est réalisée
            try
            {
                var content = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(pwd));
                StringBuilder sb = new StringBuilder();
                foreach (byte b in content)
                    sb.Append(b.ToString("x2"));
                string hash = sb.ToString().ToLower();

                if (login == "admin" && hash == "73CD1B16C4FB83061AD18A0B29B9643A68D4640075A466DC9E51682F84A847F5") // SECU (A02:2021-Cryptographic Failures) : facile à trouver que c'est le hash de superman et MD5 est obsolète
                    isAuthenticated = true;
                else if (login != "admin")
                {
                    using (var conn = new SqliteConnection("Data Source=test.db"))
                    {
                        conn.Open();
                        var commande = conn.CreateCommand();
                        commande.CommandText = "SELECT hash FROM USERS WHERE login='" + login + "'";
                        if (commande.ExecuteScalar()?.ToString() == hash) // SECU (A01:2021-Broken Access Control) : si on génère une exception en injectant un login avec une apostrophe, par exemple, alors on passe en exception et on considère qu'on est authentifié
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
                // SECU (A09:2021-Security Logging and Monitoring Failures) : il faut loguer aussi et même surtout les erreurs d'authentification
                _logger.LogCritical("Tentative de connexion avec le login : " + login);
                return new UnauthorizedResult();
            }
        }

        [HttpGet("validate")]
        public StatusCodeResult ValidateUsersList(string xmlContent)
        {
            // SECU (A08:2021-Software and Data Integrity Failures) : potentielle attaque par XML bombing, si on laisse du contenu entrer tel quel et qu'on ne met pas de validation ou de limite sur les ressources (mais pas facile à blinder)

            XmlReaderSettings settings = new XmlReaderSettings();
            settings.DtdProcessing = DtdProcessing.Parse;
            settings.ValidationType = ValidationType.DTD;
            settings.MaxCharactersFromEntities = 300;
            try
            {
                XmlReader reader = XmlReader.Create(new StringReader(xmlContent), settings);
                var i = 0;
                while (reader.Read())
                {
                    // loop on users!
                    if (reader.NodeType == XmlNodeType.Element && reader.Name == "users") i++;
                }
                if (i > 0)
                {
                    return new OkResult();
                }
            }
            catch (XmlException ex)
            {
                Console.WriteLine(ex.Message);
                return new StatusCodeResult(503);
            }

            return NotFound();
        }
    }
}
