using Blazored.SessionStorage;
using Microsoft.AspNetCore.Components.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace BadSecApp.Client
{
    public class LocalAuthenticationStateProvider : AuthenticationStateProvider
    {
        private const string authenticationKey = "authentification";
        private readonly HttpClient httpClient;
        private readonly ISessionStorageService sessionStorage;

        public LocalAuthenticationStateProvider(HttpClient httpClient, ISessionStorageService sessionStorage)
        {

            this.httpClient = httpClient;
            this.sessionStorage = sessionStorage;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            // Récupération du token à partir de la session storage
            string token = await sessionStorage.GetItemAsync<string>(authenticationKey);

            // Si le token est vide alors on retourne un utilisateur anonyme dans l'authentication state
            if (string.IsNullOrWhiteSpace(token) || !IsValidToken(token))
            {
                httpClient.DefaultRequestHeaders.Authorization = null;
                await sessionStorage.RemoveItemAsync(authenticationKey);
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            // Sinon on met un entête HTTP de type Authorization avec comme clé "bearer" et comme valeur le token
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);

            return await CreateAuthState(token);
        }

        public async Task MarkUserAsAuthenticatedAsync(string jwt)
        {
            // on enregiste le token dans la session storage
            await sessionStorage.SetItemAsync(authenticationKey, jwt);

            // on crée un Athentication state qui contient les claims de l'utilisateur à partir du token
            Task<AuthenticationState> authState = CreateAuthState(jwt);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", jwt);

            // On notifie du changement de statut d'authentification
            NotifyAuthenticationStateChanged(authState);
        }

        public async Task MarkUserAsLoggedOutAsync()
        {
            // On supprime le token JWT de la session storage
            await sessionStorage.RemoveItemAsync(authenticationKey);

            // On crée un Authentication state contenant un utilisateur anonyme
            Task<AuthenticationState> authState = Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));

            // On enlève l'entête d'autorisation du client HTTP
            httpClient.DefaultRequestHeaders.Authorization = null;

            // On notifie du changement de statut d'authentification
            NotifyAuthenticationStateChanged(authState);
        }

        private static Task<AuthenticationState> CreateAuthState(string jwt)
        {
            //Récupération des claims à partir du token JWT
            var claims = JWTParser.ParseClaimsFromJwt(jwt);

            //Création de l'identité à partir des claims
            ClaimsIdentity identities = new(claims, "jwtAuthType");

            //Création du principal(utilisateur) à partir de l'identité
            ClaimsPrincipal authenticatedUser = new(identities);

            return Task.FromResult(new AuthenticationState(authenticatedUser));
        }

        private static bool IsValidToken(string jwt)
        {
            //Récupération des claims à partir du token JWT
            var claims = JWTParser.ParseClaimsFromJwt(jwt);

            //Récupération de la date d'expiration du token
            var expiry = claims.Where(claim => claim.Type.Equals("exp")).FirstOrDefault();

            var tokenExpired = false;
            if (expiry != null)
            {
                // Le champ exp est sous format temps Unix
                var datetime = DateTimeOffset.FromUnixTimeSeconds(long.Parse(expiry.Value));
                if (datetime.UtcDateTime <= DateTime.UtcNow)
                    tokenExpired = true;
            }

            return !tokenExpired;
        }
    }

    public static class JWTParser
    {
        public static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
        {
            //Format d'aun token JWT : Header.Payload(en base 64).SigningKey
            var claims = new List<Claim>();
            var payload = jwt.Split('.')[1];

            var jsonBytes = ParseBase64WithoutPadding(payload);

            var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

            claims.AddRange(keyValuePairs.Select(kvp => new Claim(kvp.Key, kvp.Value.ToString())));

            return claims;
        }

        private static byte[] ParseBase64WithoutPadding(string base64)
        {
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            return Convert.FromBase64String(base64);
        }
    }

}
