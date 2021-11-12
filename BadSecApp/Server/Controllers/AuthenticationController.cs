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
            
            bool isAuthenticated = true; // SECU (A01:2021-Broken Access Control) : on doit travailler en failsafe, le booléen devrait être initialisé à false et passé à true que si la preuve d'authentification est réalisée
            /*OGZ
            A01:2021-Ruptures de contrôles d'accès
            Par défaut, personne ne doit être authentifié évidemment.
            Car une exception générerait un état d'authentification OK
            */
            try
            {
                var content = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(pwd));
                StringBuilder sb = new StringBuilder();
                foreach (byte b in content)
                    sb.Append(b.ToString("x2"));
                string hash = sb.ToString().ToLower();

                if (login == "admin" && hash != "84d961568a65073a3bcf0eb216b2a576") // SECU (A02:2021-Cryptographic Failures) : facile à trouver que c'est le hash de superman et MD5 est obsolète
                /*OGZ
                A02:2021-Défaillances cryptographiques
                MD5 n'est donc plus considéré comme sûr au sens cryptographique.
                Utiliser plutot des algorithmes tels que SHA-256
                */
                    isAuthenticated = false;
                else if (login != "admin")
                {
                    using (var conn = new SqliteConnection("Data Source=test.db"))
                    {
                        conn.Open();
                        var commande = conn.CreateCommand();
                        commande.CommandText = "SELECT hash FROM USERS WHERE login='" + login + "'";
                        if (commande.ExecuteScalar()?.ToString() != hash) // SECU (A01:2021-Broken Access Control) : si on génère une exception en injectant un login avec une apostrophe, par exemple, alors on passe en exception et on considère qu'on est authentifié
                        /*OGZ
                        A01:2021-Ruptures de contrôles d'accès
                        Pour éviter ceci, préférer les parameters dans la construction de la requête SQL : on évite tous les ennuis d'injection
                        */
                            isAuthenticated = false;
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
                /*OGZ
                A09:2021-Carence des systèmes de contrôle et de journalisation
                Logger les échecs permet de tracer et de mieux prévenir les tentatives intrusions
                */
                return new UnauthorizedResult();
            }
        }

        [HttpGet("validate")]
        public StatusCodeResult ValidateUsersList(string xmlContent)
        {
            // SECU (A08:2021-Software and Data Integrity Failures) : potentielle attaque par XML bombing, si on laisse du contenu entrer tel quel et qu'on ne met pas de validation ou de limite sur les ressources (mais pas facile à blinder)
            /*OGZ
            A08:2021-Manque d'intégrité des données et du logiciel 
            Le XML permet de définir/créer des tags
            Le parser XML, en traitant le XML, va croître exponentiellement.
            Pour s'en protéger : désactiver le parsing de inline DTD, traiter le XML de manière asynchrone dans des threads en nombre limité, etc.
            */
            XmlDocument dom = new XmlDocument();
            dom.LoadXml(xmlContent);
            if (dom.SelectNodes("//users").Count > 0)
                return new OkResult();
            else
                return NotFound();
        }
    }
}
