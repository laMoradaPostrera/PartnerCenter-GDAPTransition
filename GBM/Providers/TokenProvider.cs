using Microsoft.Identity.Client;
using PartnerLed.Model;

namespace PartnerLed.Providers
{
    internal class TokenProvider : ITokenProvider
    {

        // <summary>
        /// TokenProvider constructor.
        /// </summary>
        /// <param name="appSettings">The app settings.</param>
        public TokenProvider(AppSetting appSetting) => tokenAcquisitionHelper = new PublicAppUsingInteractive(appSetting.InteractiveApp);

        protected PublicAppUsingInteractive tokenAcquisitionHelper;
        private AuthenticationResult authenticationResult, graphAuthenticationResult;

        private string partnerTenantId { get; set; }

        /// <summary>
        /// Scopes to request access to the protected Web API
        /// </summary>
        private static string[] Scopes { get; } = new string[] {
            "https://api.partnercustomeradministration.microsoft.com/PartnerCustomerDelegatedAdministration.ReadWrite.All" };

        /// <summary>
        /// Scopes to request access to the protected Web API (here Microsoft Graph)
        /// </summary>
        private static string[] ScopesGraph { get; } = new string[] {
            "https://graph.microsoft.com/Group.Read.All" };


        public string getPartnertenantId() => partnerTenantId;

        private bool validateToken(AuthenticationResult result)
        {
            if (result != null && result.ExpiresOn > DateTimeOffset.UtcNow.AddMinutes(5))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public async Task<AuthenticationResult?> GetTokenAsync(Resource type)
        {
            AuthenticationResult? result = null;
            try
            {
                switch (type)
                {
                    case Resource.TrafficManager:
                        if (validateToken(authenticationResult))
                        {
                            return authenticationResult;
                        }
                        result = await AcquireTokenAsync(Scopes);
                        authenticationResult = result;
                        break;

                    case Resource.GraphManager:
                        if (validateToken(graphAuthenticationResult))
                        {
                            return graphAuthenticationResult;
                        }

                        result = await AcquireTokenAsync(ScopesGraph);
                        graphAuthenticationResult = result;
                        break;
                }
            }
            catch
            {
                Console.WriteLine("Failed to authenticate.");
            }

            return result;
        }

        public async Task<AuthenticationResult> AcquireTokenAsync(string[] scope)
        {

            AuthenticationResult result;
            try
            {
                Console.WriteLine("Authenticating: Login via Web browser");
                result = await tokenAcquisitionHelper.AcquireATokenFromCacheOrInteractivelyAsync(scope);
            }
            catch
            {
                Console.WriteLine($"Exception while generating token");
                throw;
            }
            partnerTenantId = result.TenantId;
            return result;
        }

    }
}
