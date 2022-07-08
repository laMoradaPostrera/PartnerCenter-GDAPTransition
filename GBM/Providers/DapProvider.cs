using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PartnerLed.Model;
using PartnerLed.Utility;
using System.Net;

namespace PartnerLed.Providers
{
    internal class DapProvider : IDapProvider
    {
        /// <summary>
        /// Provider Instances.
        /// </summary>
        private readonly ITokenProvider tokenProvider;
        private readonly ILogger<DapProvider> logger;
        private readonly IGdapProvider gdapProvider;
        private readonly IExportImportProviderFactory exportImportProviderFactory;
        private readonly IAccessAssignmentProvider accessAssignmentProvider;

        protected ProtectedApiCallHelper protectedApiCallHelper;

        private string GdapBaseEndpoint { get; set; }

        /// <summary>
        /// URLs of the protected Web APIs to call GDAP (here Traffic Manager endpoints)
        /// </summary>
        private string WebApiUrlAllDaps { get { return $"{GdapBaseEndpoint}/v1/delegatedAdminCustomers"; } }

        /// <summary>
        /// DAP provider constructor.
        /// </summary>
        public DapProvider(ITokenProvider tokenProvider, AppSetting appSetting, IExportImportProviderFactory exportImportProviderFactory, IGdapProvider gdapProvider,
            IAccessAssignmentProvider accessAssignmentProvider, ILogger<DapProvider> logger)
        {
            this.tokenProvider = tokenProvider;
            this.logger = logger;
            this.exportImportProviderFactory = exportImportProviderFactory;
            this.gdapProvider = gdapProvider;
            this.accessAssignmentProvider = accessAssignmentProvider;
            protectedApiCallHelper = new ProtectedApiCallHelper(appSetting.Client);
            GdapBaseEndpoint = appSetting.GdapBaseEndPoint;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<bool> ExportCustomerDetails(ExportImport type)
        {
            var authenticationResult = await tokenProvider.GetTokenAsync(Resource.TrafficManager);
            var exportImportProvider = exportImportProviderFactory.Create(type);
            if (authenticationResult != null)
            {
                Console.WriteLine("Getting customers...");
                string accessToken = authenticationResult.AccessToken;
                var url = $"{WebApiUrlAllDaps}?$count=true&$filter=dapEnabled+eq+true&$orderby=organizationDisplayName"; ;
                try
                {
                    var response = await protectedApiCallHelper.CallWebApiAndProcessResultAsync("https://traf-pcsvcadmin-prod.trafficmanager.net/CustomerServiceAdminApi/Web//v1/delegatedAdminCustomers?$count=true&$filter=dapEnabled+eq+true&$orderby=organizationDisplayName", accessToken);
                    if (response.IsSuccessStatusCode)
                    {
                        var result = JsonConvert.DeserializeObject(response.Content.ReadAsStringAsync().Result) as JObject;
                        var allCustomer = new List<DelegatedAdminRelationshipRequest>();
                        var partnerTenantId = tokenProvider.getPartnertenantId();

                        Console.WriteLine("Downloading customers..");
                        foreach (JProperty child in result.Properties().Where(p => !p.Name.StartsWith("@")))
                        {
                            foreach (var item in child.Value)
                            {
                                var dapCustomer = item.ToObject<DelegatedAdminCustomer>();
                                allCustomer.Add(new DelegatedAdminRelationshipRequest() { CustomerTenantId = dapCustomer.CustomerTenantId, OrganizationDisplayName = dapCustomer.OrganizationDisplayName, PartnerTenantId = partnerTenantId });
                            }
                        }
                        var path = $"{Constants.OutputFolderPath}/customers";
                        await exportImportProvider.WriteAsync(allCustomer, $"{path}.{Helper.GetExtenstion(type)}");
                        Console.WriteLine($"Downloaded customers at {Constants.OutputFolderPath}/customers.{Helper.GetExtenstion(type)}");
                    }
                    else 
                    {
                        string userResponse = GetUserResponse(response.StatusCode);
                        Console.WriteLine($"{userResponse}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error while exporting customer Details");
                    logger.LogError(ex.Message);
                }

            }
            return true;
        }

        private string GetUserResponse(HttpStatusCode statusCode)
        {
            switch (statusCode)
            {
                case HttpStatusCode.Unauthorized: return "Authentication Failed. Please make sure your Sign-in credentials are MFA enabled.";
                default: return "Failed to get Customer details.";
            }
        }

        public async Task<bool> GenerateDAPRelatioshipwithAccessAssignment(ExportImport type)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\t");
            Console.WriteLine("Warning: To run this operation, please make sure all the files should be in the input folder mentioned below");
            Console.WriteLine($"{Constants.InputFolderPath}/gdapRelationship");
            Console.ResetColor();
            Console.WriteLine("Press [y/Y] to continue or any other key to exit the operation.");
            var option = Console.ReadLine();
            try
            {
                if (option == "Y" || option == "y")
                {
                    TimeSpan ts = new TimeSpan(0, 0, 5);
                    TimeSpan ts1 = new TimeSpan(0, 0, 10);

                    await gdapProvider.CreateGDAPRequestAsync(type);
                    //Console.WriteLine($"Sleeping the Thread for {ts1.TotalSeconds}");
                    Thread.Sleep(ts1);
                    await gdapProvider.RefreshGDAPRequestAsync(type);
                    Thread.Sleep(ts);
                    await accessAssignmentProvider.CreateAccessAssignmentRequestAsync(type);
                    Thread.Sleep(ts);
                    await accessAssignmentProvider.RefreshAccessAssignmentRequest(type);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }
            return true;
        }
    }
}
