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
            
            bool isAuthenticated = true; // SECU : on doit travailler en failsafe, le booléen devrait être initialisé à false et passé à true que si la preuve d'authentification est réalisée
            try
            {
                var content = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(pwd));
                StringBuilder sb = new StringBuilder();
                foreach (byte b in content)
                    sb.Append(b.ToString("x2"));
                string hash = sb.ToString().ToLower();

                if (login == "admin" && hash != "84d961568a65073a3bcf0eb216b2a576") // SECU : facile à trouver que c'est le hash de superman et MD5 est obsolète
                    isAuthenticated = false;
                else if (login != "admin")
                {
                    using (var conn = new SqliteConnection("Data Source=test.db"))
                    {
                        conn.Open();
                        var commande = conn.CreateCommand();
                        commande.CommandText = "SELECT hash FROM USERS WHERE login='" + login + "'";
                        if (commande.ExecuteScalar()?.ToString() != hash) // SECU : si on génère une exception en injectant un login avec une apostrophe, par exemple, alors on passe en exception et on considère qu'on est authentifié
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
                return new UnauthorizedResult();
            }
        }
    }
}
