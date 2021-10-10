using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Unicode;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace BadSecApp.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthenticationController : Controller
    {
        private const string SALT = "xe3Y)`?&"; // We could generate a dynamic hash and store it to db
        private readonly ILogger<AuthenticationController> _logger;

        public AuthenticationController(ILogger<AuthenticationController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public StatusCodeResult Login([FromQuery] string login, [FromQuery] string pwd)
        {
            if (string.IsNullOrWhiteSpace(login)) throw new ArgumentException("wrong auth"); // Return generic information on auth process
            // pwd can't be null
            if (string.IsNullOrWhiteSpace(pwd)) throw new ArgumentException("wrong auth"); // Return generic information on auth process

            // A07:2021 – Identification and Authentication Failures => Pasword is weak (superman)
            // Add defensive code n°1 : Check password complexity : Normally should be done only on account creation (complexity rule can change over the time)
            //• at least 8 chars
            //• at least 1 lowercase char
            //• at least 1 uppercase char
            //• at least 1 number
            //• special chars allowed
            var pwdComplexity = new Regex(@"(?=^.{8,}$)(?=.*\d)(?=.*[a-z])(?=.*[A-Z])(?!.*\s)[0-9a-zA-Z!@#$%^&amp;*()_+}{&quot;:;'?/&gt;.&lt;,]*$");
            if (!pwdComplexity.IsMatch(pwd))
                throw new ArgumentException("wrong auth"); // Return generic information on auth process

            // Add defensive code n°2 (not really relevant since we are using Sql Parameters below)
            if (login.Contains("--"))
                throw new ArgumentException("wrong auth"); // Return generic information on auth process

            // We could add more defensive code here ...

            // A01:2021–Broken Access Control
            // A07:2021-Identification and Authentication Failures => Default value should be false
            bool isAuthenticated = false;
            try
            {
                // Enforce security password with salt
                var saltedPwd = pwd + SALT;

                // A02:2021-Cryptographic Failures => MD5 is weak, use SHA256 that has not known collision (yet !)
                var content = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(saltedPwd));

                // This code convert all chars in hexa, I didn't check but we may loose something during the convertion ...
                StringBuilder sb = new StringBuilder();
                foreach (byte b in content)
                    sb.Append(b.ToString("x2"));
                string hash = sb.ToString().ToLower();

                // A04:2021–Insecure Design => Get rid of hardcoded auth
                using (var conn = new SqliteConnection("Data Source=test.db"))
                {
                    conn.Open();
                    using (var commande = conn.CreateCommand())
                    {
                        // A03:2021-Injection : Sql Injection => Use Sql Parameters
                        commande.Parameters.AddWithValue("login", login);
                        commande.Parameters.AddWithValue("hash", hash);
                        commande.CommandText = "SELECT COUNT(*) FROM USERS WHERE login=@login and hash=@hash"; // Query using login AND Hash
                        var res = (long)commande.ExecuteScalar();
                        if (res == 0)
                            _logger.LogDebug($"Wrong Auth with credentials ({login}/{pwd})");
                        else if (res > 1)
                            _logger.LogDebug($"Auth with credentials ({login}/{pwd}), returned {res} users !");
                        else if (res == 1) // Return true only if we have only one user with this credentials
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
                // A09:2021-Security Logging and Monitoring Failures => Log auth failure
                _logger.LogDebug($"Login failure with credentials ({login}/{pwd})");

                // Add a fail auth counter and disallow connection for 5 min after 3 fails

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
            //else
            return NotFound();
        }
    }
}