using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using System.Threading.Tasks;
using System.Xml;

namespace BadSecApp.Server.Controllers
{
    [AllowAnonymous]
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

        [HttpGet("validate")]
        public StatusCodeResult ValidateUsersList(string xmlContent)
        {
            int Compteur = 0;
            Stopwatch chrono = Stopwatch.StartNew();
            using (XmlReader lecteur = XmlReader.Create(new StringReader(xmlContent)))
            {
                lecteur.ReadStartElement("users");
                while (lecteur.Read() && chrono.ElapsedMilliseconds < 3000)
                {
                    if (lecteur.NodeType == XmlNodeType.EndElement && lecteur.Name == "users") break;
                    else Compteur++;
                }
            }

            if (Compteur > 0)
                return new OkResult();
            else
                return NotFound();
        }

        [HttpPost]
        [Route("login")]
        public ActionResult Login(CallConnexion connInfo)
        {
            if (connInfo is null) throw new ArgumentException("Connection information cannot be empty");
            if (connInfo.Login is null) throw new ArgumentException("Login cannot be empty");
            if (connInfo.Password is null) throw new ArgumentException("Password cannot be empty");

            bool isAuthenticated = false;
            try
            {
                var salt = _config.GetValue<string>("sel-pour-mots-de-passe"); // Dans une approche simple, on passe le sel "Zorglub64" en variable d'environnement, mais on pourrait durcir bien sûr encore plus
                var content = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(string.Concat(salt, connInfo.Password)));
                StringBuilder sb = new StringBuilder();
                foreach (byte b in content)
                    sb.Append(b.ToString("x2"));
                string hash = sb.ToString().ToLower();

                if (connInfo.Login == "admin" && hash == "b00d11bc27e970579921c0775b7c15599224b606bb2a582bf94952c956b5a679")
                    isAuthenticated = true;
                else if (connInfo.Login != "admin")
                {
                    using (var conn = new SqliteConnection("Data Source=test.db"))
                    {
                        conn.Open();
                        var commande = conn.CreateCommand();
                        commande.CommandText = "SELECT hash FROM USERS WHERE login=@login";
                        commande.Parameters.Add(new SqliteParameter("login", connInfo.Login));
                        if (commande.ExecuteScalar()?.ToString() == hash)
                            isAuthenticated = true;
                    }
                }
            }
            catch (Exception excep)
            {
                _logger.LogDebug(excep.ToString());
            }

            if (!isAuthenticated)
            {
                _logger.LogWarning("Tentative d'authentification incorrecte sur le login " + connInfo.Login);
                return Unauthorized();
            }

            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, connInfo.Login),
                new Claim(ClaimTypes.GivenName, connInfo.Login == "admin" ? "Respected administrator" : "Dear user " + connInfo.Login),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            if (connInfo.Login == "admin")
                authClaims.Add(new Claim(ClaimTypes.Role, "administrator"));

            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JWT:Secret"]));

            var token = new JwtSecurityToken(
                issuer: _config["JWT:ValidIssuer"],
                audience: _config["JWT:ValidAudience"],
                expires: DateTime.Now.AddHours(4),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );

            _logger.LogInformation("Authentification correcte du login " + connInfo.Login);
            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                expiration = token.ValidTo
            });
        }
    }

    public class CallConnexion
    {
        public string Login { get; set; }
        public string Password { get; set; }
    }
}
