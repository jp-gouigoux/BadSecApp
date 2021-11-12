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
            // A07:2021 – Identification and Authentication Failures : Permits default, weak, or well-known passwords

            if (login is null) throw new ArgumentException("login cannot be empty");
            if (pwd is null) pwd = string.Empty;

            // A01 deny by default : Mettre false par défaut
            bool isAuthenticated = false;
            try
            {
                // A02 Cryptographic Failures : MD5 - deprecated hash function : using strong adaptive and salted hashing functions with a work factor (delay factor), such as Argon2, scrypt, bcrypt or PBKDF2.
                var content = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(pwd));
                StringBuilder sb = new StringBuilder();
                foreach (byte b in content)
                    sb.Append(b.ToString("x2"));
                string hash = sb.ToString().ToLower();

                // A02 Crypto keys checked into source code repositories : Admin devrait être aussi en base               
                using (var conn = new SqliteConnection("Data Source=test.db"))
                {
                    conn.Open();
                    var commande = conn.CreateCommand();
                    // A03:2021 – Injection SQL
                    commande.CommandText = "SELECT hash FROM USERS WHERE login= @login";
                    commande.Parameters.AddWithValue("login", login);

                    if (commande.ExecuteScalar()?.ToString() != hash)
                    {
                        isAuthenticated = false;
                    }
                    else
                    {
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
                // A05:2021 – Security Misconfiguration : Sensitive Cookie in HTTPS Session Without 'Secure' Attribute.
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
