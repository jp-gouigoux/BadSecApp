using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BadSecApp.Server
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            // SECU (A04:2021-Insecure Design) : voir plus bas
            services.AddSession();

            services.AddControllersWithViews();
            services.AddRazorPages();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            using (var conn = new SqliteConnection("Data Source=test.db"))
            {
                conn.Open();
                var commande = conn.CreateCommand();
                commande.CommandText = "DROP TABLE IF EXISTS PERSONNES; CREATE TABLE PERSONNES (nom VARCHAR(32), prenom VARCHAR(32), age INT)";
                commande.ExecuteNonQuery();

                conn.Open();
                commande = conn.CreateCommand();
                commande.CommandText = "DROP TABLE IF EXISTS PHOTOS; CREATE TABLE PHOTOS (nom VARCHAR(32), url VARCHAR(32))";
                commande.ExecuteNonQuery();

                commande = conn.CreateCommand();
                commande.CommandText = "DROP TABLE IF EXISTS CONTRATS; CREATE TABLE CONTRATS (entreprise VARCHAR(32), sujet VARCHAR(32), montant INT)";
                commande.ExecuteNonQuery();

                commande = conn.CreateCommand();
                commande.CommandText = "DROP TABLE IF EXISTS USERS; CREATE TABLE USERS (login VARCHAR(32), hash VARCHAR(32))";
                commande.ExecuteNonQuery();

                commande = conn.CreateCommand();
                commande.CommandText = "INSERT INTO PERSONNES (nom, prenom, age) VALUES ('Lagaffe', 'Gaston', 63)";
                commande.ExecuteNonQuery();

                commande = conn.CreateCommand();
                commande.CommandText = "INSERT INTO PHOTOS (nom, url) VALUES ('Lagaffe', 'gaston.png')";
                commande.ExecuteNonQuery();

                commande = conn.CreateCommand();
                commande.CommandText = "INSERT INTO CONTRATS (entreprise, sujet, montant) VALUES ('Minist�re', 'Contrat ultra-secret', 1000000)";
                commande.ExecuteNonQuery();

                commande = conn.CreateCommand();
                commande.CommandText = "INSERT INTO USERS (login, hash) VALUES ('user', 'f71dbe52628a3f83a77ab494817525c6')"; // SECU (A02:2021-Cryptographic Failures) : donn�e sensible pas assez obfusqu�e (MD5 de toto) et pr�sente dans le code, donc dans le GitHub visible de tous
                commande.ExecuteNonQuery();
            }

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseWebAssemblyDebugging();
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseBlazorFrameworkFiles();
            app.UseStaticFiles();

            // SECU (A04:2021-Insecure Design) : inactif par d�faut, toute augmentation de la surface d'attaque peut poser probl�me (ainsi que pour la mont�e en charge, dans ce cas pr�cis, � cause des affinit�s de sessions)
            app.UseSession();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllers();
                endpoints.MapFallbackToFile("index.html");
            });
        }
    }
}
