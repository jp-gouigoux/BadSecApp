using BadSecApp.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        public List<Personne> GetAll([FromQuery] string IndicationNom)
        {
            var donnees = new List<Personne>();

            using (var conn = new SqliteConnection("Data Source=test.db"))
            {
                conn.Open();
                var commande = conn.CreateCommand();
                commande.CommandText = "SELECT p.nom nom, p.prenom prenom, p.age age, h.url url FROM PERSONNES p INNER JOIN PHOTOS h ON p.nom = h.nom WHERE p.nom LIKE @chaine";
                commande.Parameters.Add(new SqliteParameter("chaine", IndicationNom + "%"));

                using (var reader = commande.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        donnees.Add(
                            new Personne()
                            {
                                Nom = reader.GetString(0),
                                Prenom = reader.GetString(1),
                                Age = reader.GetInt32(2),
                                UrlPhoto = reader.GetString(3)
                            });
                    }
                }
            }
            return donnees;
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
                        sb.Append("<h1>").Append(HttpUtility.HtmlEncode(nom)).Append(" ne fait pas partie de notre annuaire !").AppendLine("</h1>");
                    }
                }

                commande = conn.CreateCommand();
                commande.CommandText = "SELECT url FROM PHOTOS WHERE nom=@nom";
                commande.Parameters.Add(new SqliteParameter("nom", nom));
                using (var reader = commande.ExecuteReader())
                {
                    if (reader.Read())
                        sb.Append("<img src=\"").Append(HttpUtility.HtmlEncode(reader.GetString(0))).AppendLine("\"/>");
                }
            }

            sb.AppendLine("</body>");
            sb.AppendLine("</html>");
            return Content(sb.ToString(), "text/html", Encoding.UTF8);
        }
    }
}
