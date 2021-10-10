using BadSecApp.Shared;
using Microsoft.AspNetCore.Components;
using System.Net.Http;
using System.Web;

namespace BadSecApp.Client.Pages
{
    public class LoginBase : ComponentBase
    {
        protected Credentials ProposedCredentials { get; set; }

        protected string resultat;

        [Inject]
        protected HttpClient http { get; set; }

        protected override void OnInitialized()
        {
            ProposedCredentials = new Credentials();
            base.OnInitialized();
        }

        protected async void Creer()
        {
            HttpResponseMessage retour = await http.GetAsync("api/Authentication?login=" + ProposedCredentials.login + "&pwd=" + ProposedCredentials.pwd);
            resultat = retour.IsSuccessStatusCode ? "Vous êtes connecté en tant que " + HttpUtility.HtmlEncode(ProposedCredentials.login) : "Authentification incorrecte"; // Fix XSS injection
            this.StateHasChanged();

            Shared.NavMenu.SetMenusVisibility(
                MenuPersonnesVisible: retour.IsSuccessStatusCode, 
                MenuContratsVisible: retour.IsSuccessStatusCode); // Remove this from client side, That should never be handled by the client app, if you need to allow things based on user rôle implements authorization with OpenId, or LDAP, ... !
        }
    }
}
