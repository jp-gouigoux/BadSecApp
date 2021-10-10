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
                    using (var commande = conn.CreateCommand())
                    {
                        // A03:2021-Injection : Sql Injection => Use Sql Parameters
                        commande.Parameters.AddWithValue("name", personne.Nom);
                        commande.Parameters.AddWithValue("firstname", personne.Prenom);
                        commande.Parameters.AddWithValue("age", personne.Age);
                        commande.CommandText = "INSERT INTO PERSONNES (nom, prenom, age) VALUES (@name, @firstname, @age)";
                        commande.ExecuteNonQuery();

                        commande.CommandText = "INSERT INTO PHOTOS (nom, url) VALUES (@nom, @url)";
                        commande.Parameters.Add(new SqliteParameter("nom", personne.Nom));
                        commande.Parameters.Add(new SqliteParameter("url", personne.UrlPhoto));
                        commande.ExecuteNonQuery();
                    }
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
                    using (var commande = conn.CreateCommand())
                    {
                        // A03:2021-Injection : Sql Injection => Use Sql Parameters
                        commande.Parameters.AddWithValue("indicationNom", $"%{IndicationNom}%");
                        commande.CommandText = "SELECT nom, prenom, age FROM PERSONNES WHERE nom LIKE @indicationNom";

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
                    }

                    foreach (Personne p in donnees)
                    {
                        using (var commande = conn.CreateCommand())
                        {
                            commande.Parameters.AddWithValue("name", p.Nom);
                            commande.CommandText = "SELECT url FROM PHOTOS WHERE nom=@name";
                            using (var reader = commande.ExecuteReader())
                            {
                                reader.Read();
                                p.UrlPhoto = reader.GetString(0);
                            }
                        }
                    }
                }
            }
            catch
            {
                // 
                erreur = "An error occured";
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
                using (var commande = conn.CreateCommand())
                {
                    // A03:2021-Injection : Sql Injection => Use Sql Parameters
                    commande.CommandText = "SELECT prenom, age FROM PERSONNES WHERE nom=@nom";
                    commande.Parameters.Add(new SqliteParameter("nom", nom));
                    using (var reader = commande.ExecuteReader())
                    {
                        // A03:2021-Injection : XSS Injection => Use HttpEncode
                        var input = HttpUtility.HtmlEncode(nom);
                        var name = HttpUtility.HtmlEncode(reader.GetString(0));
                        var age = HttpUtility.HtmlEncode(reader.GetInt32(1).ToString());

                        if (reader.Read())
                        {
                            sb.Append("<h1>").Append(name).Append(" ").Append(input).AppendLine("</h1>");
                            sb.Append("<p>Agé.e de ").Append(age).AppendLine(" ans</p>");
                        }
                        else
                        {
                            sb.Append("<h1>").Append(input).Append(" ne fait pas partie de notre annuaire !").AppendLine("</h1>");
                        }
                    }
                }

                using (var commande = conn.CreateCommand())
                {
                    // A03:2021-Injection : Sql Injection => Use Sql Parameters
                    commande.CommandText = "SELECT url FROM PHOTOS WHERE nom=@nom";
                    commande.Parameters.Add(new SqliteParameter("nom", nom));
                    using (var reader = commande.ExecuteReader())
                    {
                        if (reader.Read())
                            sb.Append("<img src=\"").Append(HttpUtility.HtmlEncode(reader.GetString(0))).AppendLine("\"/>"); // A03:2021-Injection : XSS Injection => Use HttpEncode
                    }
                }
            }

            sb.AppendLine("</body>");
            sb.AppendLine("</html>");
            return Content(sb.ToString(), "text/html", Encoding.UTF8);
        }
    }
}
