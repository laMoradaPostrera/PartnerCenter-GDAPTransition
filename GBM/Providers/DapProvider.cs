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
        protected string WebApiUrlAllDaps { get { return $"{GdapBaseEndpoint}/v1/delegatedAdminCustomers"; } }

        /// <summary>
        /// URLs of the protected Web APIs to call GDAP (here Traffic Manager endpoints)
        /// </summary>
        protected string WebApiUrlAllDapsBulk { get { return $"{GdapBaseEndpoint}/v1/delegatedAdminCustomersBulk"; } }

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
        /// Export customer Details based on user selection of type
        /// </summary>
        /// <param name="type">Export type "Json" or "Csv" based on user selection.</param>
        /// <returns></returns>
        public async Task<bool> ExportCustomerDetails(ExportImport type)
        {
            var authenticationResult = await tokenProvider.GetTokenAsync(Resource.TrafficManager);
            var exportImportProvider = exportImportProviderFactory.Create(type);
            if (authenticationResult != null)
            {
                Console.WriteLine("Getting customers...");
                string accessToken = authenticationResult.AccessToken;
                var url = $"{WebApiUrlAllDaps}?$count=true&$filter=dapEnabled+eq+true&$orderby=organizationDisplayName";

                var nextLink = string.Empty;

                List<DelegatedAdminRelationshipRequest>? allCustomer = new List<DelegatedAdminRelationshipRequest>();
                try
                {
                    Console.WriteLine("Downloading customers..");
                    protectedApiCallHelper.setHeader(false, accessToken);
                    do
                    {
                        if (!string.IsNullOrEmpty(nextLink))
                        {
                            url = nextLink;
                        }
                        var response = await protectedApiCallHelper.CallWebApiAndProcessResultAsync(url);
                        if (response.IsSuccessStatusCode)
                        {
                            var result = JsonConvert.DeserializeObject(response.Content.ReadAsStringAsync().Result) as JObject;
                            var partnerTenantId = tokenProvider.getPartnertenantId();
                            var nextData = result.Properties().Where(p => p.Name.Contains("nextLink"));
                            nextLink = string.Empty;
                            if (nextData != null)
                            {
                                nextLink = (string?)nextData.FirstOrDefault();
                            }
                            Console.Write("..");
                            allCustomer.AddRange(GetListDelegatedRequest(result, partnerTenantId));
                            
                        }
                        else
                        {
                            string userResponse = GetUserResponse(response.StatusCode);
                            nextLink = string.Empty;
                            Console.WriteLine($"{userResponse}");
                        }
                    } while (!string.IsNullOrEmpty(nextLink));

                    var path = $"{Constants.OutputFolderPath}/customers";
                    await exportImportProvider.WriteAsync(allCustomer, $"{path}.{Helper.GetExtenstion(type)}");
                    Console.WriteLine($"\nDownloaded customers at {Constants.OutputFolderPath}/customers.{Helper.GetExtenstion(type)}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error while exporting customer Details");
                    logger.LogError(ex.Message);
                }

            }
            return true;
        }

        private List<DelegatedAdminRelationshipRequest> GetListDelegatedRequest(JObject result, string partnerTenantId) {
            var allCustomer = new List<DelegatedAdminRelationshipRequest>();
            foreach (JProperty child in result.Properties().Where(p => !p.Name.StartsWith("@")))
            {
                foreach (var item in child.Value)
                {
                    var dapCustomer = item.ToObject<DelegatedAdminCustomer>();
                    allCustomer.Add(new DelegatedAdminRelationshipRequest() { CustomerTenantId = dapCustomer.CustomerTenantId, OrganizationDisplayName = dapCustomer.OrganizationDisplayName, PartnerTenantId = partnerTenantId });
                }
            }

            return allCustomer;

        }

        private string GetUserResponse(HttpStatusCode statusCode)
        {
            switch (statusCode)
            {
                case HttpStatusCode.Unauthorized: return "Authentication Failed. Please make sure your Sign-in credentials are MFA enabled.";
                default: return "Failed to get Customer details.";
            }
        }

        /// <summary>
        /// Generate GDAP relationship with Access Assignment in one flow.
        /// </summary>
        /// <param name="type">Export type "Json" or "Csv" based on user selection.</param>
        /// <returns></returns>
        public async Task<bool> GenerateDAPRelatioshipwithAccessAssignment(ExportImport type)
        {
            var option = Helper.UserConfirmation("Warning: To run this operation, please make sure all the files should be in the input folder mentioned below");
            Console.WriteLine($"{Constants.InputFolderPath}/gdapRelationship");
            try
            {
                if (option)
                {
                    TimeSpan ts = new TimeSpan(0, 0, 5);
                    TimeSpan ts1 = new TimeSpan(0, 0, 10);

                    await gdapProvider.CreateGDAPRequestAsync(type);
                    Console.WriteLine($"\nWaiting for relationship activations... 10 seconds");
                    Thread.Sleep(ts1);
                    await gdapProvider.RefreshGDAPRequestAsync(type);

                    while (!Helper.UserConfirmation("Confirm all GDAP relationship activation complete[y/Y] or refresh again[r/R]:"))
                    {
                        await gdapProvider.RefreshGDAPRequestAsync(type);
                    }                    

                    await accessAssignmentProvider.CreateAccessAssignmentRequestAsync(type);
                    Console.WriteLine($"\nWaiting for security group-role assignment activations...");
                    Thread.Sleep(ts1);
                    await accessAssignmentProvider.RefreshAccessAssignmentRequest(type);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }
            return true;
        }

        public async Task<bool> ExportCustomerBulk()
        {
            var confirmation = Helper.UserConfirmation("Warning: This might be a long running operation.");

            if (!confirmation) {
                return true;
            }
            try
            {
                var authenticationResult = await tokenProvider.GetTokenAsync(Resource.TrafficManager);
                if (authenticationResult != null) {
                    string accessToken = authenticationResult.AccessToken;
                    var stream = await protectedApiCallHelper.CallWebApiProcessSteamAsync(WebApiUrlAllDapsBulk, accessToken);
                    SaveStreamAsFile(Constants.OutputFolderPath, stream, "customers-stream.csv.gz");
                    Console.WriteLine($"Downloaded at {Constants.OutputFolderPath}/customers - stream.csv.gz");

                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }
            return true;
        }

        private static void SaveStreamAsFile(string filePath, Stream inputStream, string fileName)
        {
            DirectoryInfo info = new DirectoryInfo(filePath);
            if (!info.Exists)
            {
                info.Create();
            }

            string path = Path.Combine(filePath, fileName);
            using (FileStream outputFileStream = new FileStream(path, FileMode.Create))
            {
                inputStream.CopyTo(outputFileStream);
            }
        }
    }
}
