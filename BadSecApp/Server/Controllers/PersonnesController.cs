using BadSecApp.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                    commande.CommandText = "INSERT INTO PERSONNES (nom, prenom, age) VALUES ('" + personne.Nom + "', '" + personne.Prenom + "', " + personne.Age.ToString() + ")"; // SECU
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
        public Tuple<List<Personne>, string> GetAll([FromQuery] string IndicationNom)
        {
            var donnees = new List<Personne>();
            string erreur = string.Empty;

            try
            {
                using (var conn = new SqliteConnection("Data Source=test.db"))
                {
                    conn.Open();
                    var commande = conn.CreateCommand();
                    commande.CommandText = "SELECT nom, prenom, age FROM PERSONNES WHERE nom LIKE '%" + IndicationNom + "%'";

                    // SECU
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

                    // SECU
                    foreach (Personne p in donnees)
                    {
                        commande = conn.CreateCommand();
                        commande.CommandText = "SELECT url FROM PHOTOS WHERE nom='" + p.Nom + "'";
                        var reader = commande.ExecuteReader();
                        reader.Read();
                        p.UrlPhoto = reader.GetString(0);
                    }
                }
            }
            catch (Exception ex)
            {
                erreur = ex.ToString();
            }
            return new Tuple<List<Personne>, string>(donnees, erreur);
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
                        // SECU
                        sb.Append("<h1>").Append(nom).Append(" ne fait pas partie de notre annuaire !").AppendLine("</h1>");
                    }
                }

                commande = conn.CreateCommand();
                commande.CommandText = "SELECT url FROM PHOTOS WHERE nom=@nom";
                commande.Parameters.Add(new SqliteParameter("nom", nom));
                using (var reader = commande.ExecuteReader())
                {
                    if (reader.Read())
                        sb.Append("<img src=\"").Append(reader.GetString(0)).AppendLine("\"/>"); // SECU
                }
            }

            sb.AppendLine("</body>");
            sb.AppendLine("</html>");
            return Content(sb.ToString(), "text/html", Encoding.UTF8);
        }
    }
}
