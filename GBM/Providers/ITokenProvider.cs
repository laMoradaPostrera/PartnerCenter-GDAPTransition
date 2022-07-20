using Microsoft.Identity.Client;
using PartnerLed.Model;

namespace PartnerLed.Providers
{
    internal interface ITokenProvider
    {
        Task<AuthenticationResult?> GetTokenAsync(Resource type);
        Task<bool> CheckPrerequisite();
        string getPartnertenantId();
    }
}
