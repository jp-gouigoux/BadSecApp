using BadSecApp.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Text;
using System.Web;

namespace BadSecApp.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PersonnesController : Controller
    {
        [HttpPost]
        public IActionResult CreationPersonne([FromBody] Personne personne)
        {
            if (!Uri.IsWellFormedUriString(personne.UrlPhoto, UriKind.Absolute))
            {
                return BadRequest("Url photo invalide");
            }

            try
            {
                using (var conn = new SqliteConnection("Data Source=test.db"))
                {
                    conn.Open();
                    var commande = conn.CreateCommand();
                    commande.CommandText = "INSERT INTO PERSONNES (nom, prenom, age) VALUES (@pNom, @pPrenom, @pAge)";
                    commande.Parameters.Add(new SqliteParameter("pNom", personne.Nom));
                    commande.Parameters.Add(new SqliteParameter("pPrenom", personne.Prenom));
                    commande.Parameters.Add(new SqliteParameter("pAge", personne.Age.ToString()));
                    commande.ExecuteNonQuery();

                    commande = conn.CreateCommand();
                    commande.CommandText = "INSERT INTO PHOTOS (nom, url) VALUES (@nom, @url)";
                    commande.Parameters.Add(new SqliteParameter("nom", personne.Nom));
                    commande.Parameters.Add(new SqliteParameter("url", personne.UrlPhoto));
                    commande.ExecuteNonQuery();
                }
                return new CreatedResult("#", personne);
            }
            catch (Exception ex)
            {
                return new UnprocessableEntityObjectResult(ex.ToString());
            }
        }

        [HttpGet]
        public IActionResult GetAll([FromQuery] string IndicationNom)
        {
            var donnees = new List<Personne>();

            // Décommenter la ligne suivante pour simuler une erreur
            //throw new Exception("doit s'afficher dans la page des exceptions en mode developer");

            using (var conn = new SqliteConnection("Data Source=test.db"))
            {
                conn.Open();
                var commande = conn.CreateCommand();

                commande.CommandText = "SELECT nom, prenom, age FROM PERSONNES WHERE nom LIKE @chaine";
                commande.Parameters.Add(new SqliteParameter("chaine", $"%{IndicationNom}%"));

                using (var reader = commande.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        donnees.Add(
                            new Personne()
                            {
                                Nom = reader.GetString(0),
                                Prenom = reader.GetString(1),
                                Age = reader.GetInt32(2)
                            });
                    }
                }

                foreach (Personne p in donnees)
                {
                    commande = conn.CreateCommand();
                    commande.CommandText = "SELECT url FROM PHOTOS WHERE nom='" + p.Nom + "'";
                    var reader = commande.ExecuteReader();
                    reader.Read();
                    p.UrlPhoto = HttpUtility.UrlEncode(reader.GetString(0));
                }
            }

            return Ok(donnees);
        }

        [HttpGet("fiche")]
        public ContentResult GenererFiche([FromQuery] string nom)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<html>");
            sb.AppendLine("<body>");

            using (var conn = new SqliteConnection("Data Source=test.db"))
            {
                conn.Open();
                var commande = conn.CreateCommand();
                commande.CommandText = "SELECT prenom, age FROM PERSONNES WHERE nom=@nom";
                commande.Parameters.Add(new SqliteParameter("nom", nom));
                using (var reader = commande.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        sb.Append("<h1>").Append(reader.GetString(0)).Append(" ").Append(nom).AppendLine("</h1>");
                        sb.Append("<p>Agé.e de ").Append(reader.GetInt32(1).ToString()).AppendLine(" ans</p>");
                    }
                    else
                    {
                        // SECU (A03:2021-Injection) : faille de Cross Site Scripting non rémanente, c'est-à-dire qu'elle nécessite que quelqu'un lance l'URL "forgée", désormais intégrée dans la même catégorie que les injections SQL et autres attaques par évitement de la forme canonique ;
                        // si on passe sur le paramètre nom une valeur bien choisie comme http://localhost:60021/api/Personnes/fiche?nom=Lagaffe%3C/h1%3E%3Cimg%20src=%22http://gouigoux.com/img/bouba.png%22%20onload=%22alert(%27owned!%27)%22/%3E%3Ch1%3E, on injecte du JavaScript qui s'exécute
                        sb.Append("<h1>").Append(nom).Append(" ne fait pas partie de notre annuaire !").AppendLine("</h1>");
                    }
                }

                commande = conn.CreateCommand();
                commande.CommandText = "SELECT url FROM PHOTOS WHERE nom=@nom";
                commande.Parameters.Add(new SqliteParameter("nom", nom));
                using (var reader = commande.ExecuteReader())
                {
                    if (reader.Read())
                        sb.Append("<img src=\"").Append(reader.GetString(0)).AppendLine("\"/>"); // SECU (A03:2021-Injection) : faille de Cross Site Scripting rémanente, et qui peut donc impacter de nombreuses personnes si on envoie la valeur "http://gouigoux.com/img/bouba.png\" onload=\"alert('owned!')" dans la base de données
                }
            }

            sb.AppendLine("</body>");
            sb.AppendLine("</html>");
            return Content(sb.ToString(), "text/html", Encoding.UTF8);
        }
    }
}
