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
            // We should never send credentials in GET ! I don't known blazor to change this : A04:2021-Insecure Design
            HttpResponseMessage retour = await http.GetAsync("api/Authentication?login=" + ProposedCredentials.login + "&pwd=" + ProposedCredentials.pwd);
            resultat = retour.IsSuccessStatusCode ? "Vous êtes connecté en tant que " + HttpUtility.HtmlEncode(ProposedCredentials.login) : "Authentification incorrecte"; // A03:2021-Injection
            this.StateHasChanged();

            Shared.NavMenu.SetMenusVisibility(
                MenuPersonnesVisible: retour.IsSuccessStatusCode,
                MenuContratsVisible: retour.IsSuccessStatusCode); // Remove this from client side, That should never be handled by the client app, if you need to allow things based on user rôle implements authorization with OpenId, or LDAP, ... !
        }
    }
}
