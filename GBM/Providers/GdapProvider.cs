// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PartnerLed.Model;
using PartnerLed.Utility;
using System.Net;

namespace PartnerLed.Providers
{
    internal class GdapProvider : IGdapProvider
    {
        /// <summary>
        /// Provider Instances
        /// </summary>
        private readonly ITokenProvider tokenProvider;
        private readonly ILogger<GdapProvider> logger;
        private readonly IExportImportProviderFactory exportImportProviderFactory;

        /// <summary>
        /// GDAP provider constructor.
        /// </summary>
        public GdapProvider(ITokenProvider tokenProvider, AppSetting appSetting, IExportImportProviderFactory exportImportProviderFactory, ILogger<GdapProvider> logger)
        {
            this.tokenProvider = tokenProvider;
            this.logger = logger;
            this.exportImportProviderFactory = exportImportProviderFactory;
            protectedApiCallHelper = new ProtectedApiCallHelper(appSetting.Client);
            GdapBaseEndpoint = appSetting.GdapBaseEndPoint;
        }

        /// <summary>
        /// Base endpoint for Traffic Manager
        /// </summary>
        private string GdapBaseEndpoint { get; set; }

        protected ProtectedApiCallHelper protectedApiCallHelper;

        /// <summary>
        /// URLs of the protected Web APIs to call GDAP (here Traffic Manager endpoints)
        /// </summary>
        private string WebApiUrlAllGdaps { get { return $"{GdapBaseEndpoint}/v1/delegatedAdminRelationships"; } }



        private async Task<DelegatedAdminRelationship> PostGdapRelationship(string url, DelegatedAdminRelationship delegatedAdminRelationship)
        {
            logger
                .LogInformation($"GDAP Request:\n{delegatedAdminRelationship.DisplayName}-{delegatedAdminRelationship.Customer.TenantId}\n{JsonConvert.SerializeObject(delegatedAdminRelationship.AccessDetails.UnifiedRoles)}");

            var response = await protectedApiCallHelper.CallWebApiPostAndProcessResultAsync(url, 
                JsonConvert.SerializeObject(delegatedAdminRelationship, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }).ToString());

            string userResponse = GetUserResponse(response.StatusCode);
            Console.WriteLine($"{delegatedAdminRelationship.DisplayName} - {userResponse}");

            logger
              .LogInformation($"GDAP Response:\n{delegatedAdminRelationship.DisplayName}-{delegatedAdminRelationship.Customer.TenantId}\n{response}");

            var relationshipObject = response.IsSuccessStatusCode
                   ? JsonConvert.DeserializeObject<DelegatedAdminRelationship>(response.Content.ReadAsStringAsync().Result)
                   : new DelegatedAdminRelationship() {DisplayName = delegatedAdminRelationship.DisplayName, 
                        Id = String.Empty, Duration = delegatedAdminRelationship.Duration,
                        Customer = new DelegatedAdminRelationshipCustomerParticipant() { DisplayName = delegatedAdminRelationship.Customer.DisplayName, TenantId = delegatedAdminRelationship.Customer.TenantId},
                        Partner = new DelegatedAdminRelationshipParticipant() {  TenantId = delegatedAdminRelationship.Partner.TenantId}
                   };

            return relationshipObject;
        }

        private string GetUserResponse(HttpStatusCode statusCode)
        {
            switch (statusCode)
            {
                case HttpStatusCode.OK:
                case HttpStatusCode.Created:
                    return "Created.";
                case HttpStatusCode.Conflict: return "GDAP Relationship name already exits.";
                case HttpStatusCode.Forbidden: return "Please check if DAP relationship exists with the Customer.";
                case HttpStatusCode.Unauthorized: return "Please make sure your Sign-in credentials are MFA enabled.";
                case HttpStatusCode.BadRequest: return "Please check input setup for Customers and ADRoles.";
                default: return "Failed. The customer does not exist or DAP relationship is missing.";
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="granularRelationshipId"></param>
        /// <param name="accessToken"></param>
        /// <returns></returns>
        private async Task<T?> GetGdapRelationship<T>(string accessToken, string granularRelationshipId = null)
        {
            var url = WebApiUrlAllGdaps;
            if (!string.IsNullOrEmpty(granularRelationshipId))
            {
                url = $"{url}/{granularRelationshipId}?PageSize=500";
            }
            else { url = $"{url}?PageSize=500"; }

            var response = await protectedApiCallHelper.CallWebApiAndProcessResultAsync(url, accessToken);
            return JsonConvert.DeserializeObject<T>(response.Content.ReadAsStringAsync().Result);
        }

        /// <summary>
        ///  Track and update the status of existing GDAP relationship status.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> RefreshGDAPRequestAsync(ExportImport type)
        {
            try
            {
                var exportImportProvider = exportImportProviderFactory.Create(type);
                var path = $"{Constants.InputFolderPath}/gdapRelationship/gdapRelationship.{Helper.GetExtenstion(type)}";
                var gDapRelationshipList = await exportImportProvider.ReadAsync<DelegatedAdminRelationship>(path);
                Console.WriteLine($"Reading files @ {path}");

                if (gDapRelationshipList == null)
                {
                    Console.WriteLine(" Error while Processing the input. No input provided. Please check the input file.");
                    Console.WriteLine($"Check the path {path}");
                }
                var inputRequest = gDapRelationshipList?.Where(x => x.Status == DelegatedAdminRelationshipStatus.Approved);
                var remainingDataList = gDapRelationshipList?.Where(x => x.Status != DelegatedAdminRelationshipStatus.Approved);

                if (inputRequest == null)
                {
                    Console.WriteLine(" Error while Processing the input. Incorrect data provided for processing. Please check the input file.");
                    Console.WriteLine($"Check the path {path}");
                }

                IEnumerable<string>? gdapId = inputRequest?.Select(p => p.Id.ToString());
                var authenticationResult = await tokenProvider.GetTokenAsync(Resource.TrafficManager);
                var responseList = new List<DelegatedAdminRelationship>();

                var tasks = gdapId
                    .Select(id => GetGdapRelationship<DelegatedAdminRelationship>(authenticationResult.AccessToken, id));
                DelegatedAdminRelationship?[] collection = await Task.WhenAll(tasks);
                responseList.AddRange(collection);


                if (remainingDataList != null)
                {
                    responseList.AddRange(remainingDataList);
                }

                Console.WriteLine($"Downloaded latest statuses of GDAP Relationship(s) at {Constants.InputFolderPath}//gdapRelationship/gdapRelationship.{Helper.GetExtenstion(type)}");
                await exportImportProvider.WriteAsync(responseList, $"{Constants.InputFolderPath}/gdapRelationship/gdapRelationship.{Helper.GetExtenstion(type)}");
            }
            catch (IOException ex)
            {
                logger.LogError(ex.Message);
                Console.WriteLine("Make sure the file is closed before running the operation.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }

            return true;
        }

        /// <summary>
        ///  Create GDAP relatioship Object.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> CreateGDAPRequestAsync(ExportImport type)
        {
            try
            {
                var exportImportProvider = exportImportProviderFactory.Create(type);
                var inputCustomer = await exportImportProvider.ReadAsync<DelegatedAdminRelationshipRequest>($"{Constants.InputFolderPath}/customers.{Helper.GetExtenstion(type)}");
                var inputAdRole = await exportImportProvider.ReadAsync<ADRole>($"{Constants.InputFolderPath}/ADRoles.{Helper.GetExtenstion(type)}");

                IEnumerable<UnifiedRole> roleList = inputAdRole.Select(x => new UnifiedRole() { RoleDefinitionId = x.Id.ToString() });

                var option = Helper.UserConfirmation($"Warning: There are {inputAdRole.Count} roles configured for creating GDAP relationship(s), are you sure you want to continue?");
                if (!option)
                {
                    return true;
                }
                var gdapList = new List<DelegatedAdminRelationship>();
                foreach (var item in inputCustomer)
                {
                    var gdapRelationship = new DelegatedAdminRelationship()
                    {
                        Customer = new DelegatedAdminRelationshipCustomerParticipant() { TenantId = item.CustomerTenantId, DisplayName = item.OrganizationDisplayName },
                        Partner = new DelegatedAdminRelationshipParticipant() { TenantId = item.PartnerTenantId.ToString() },
                        DisplayName = item.Name.ToString(),
                        Duration = $"P{item.Duration}D",
                        AccessDetails = new DelegatedAdminAccessDetails() { UnifiedRoles = roleList },
                    };
                    gdapList.Add(gdapRelationship);

                }


                var authenticationResult = await tokenProvider.GetTokenAsync(Resource.TrafficManager);
                if (authenticationResult != null)
                {
                    Console.WriteLine("Creating new relationship(s)...");
                    string accessToken = authenticationResult.AccessToken;
                    var url = $"{WebApiUrlAllGdaps}/migrate"; ;
                    var allgdapRelationList = new List<DelegatedAdminRelationship>();

                    var options = new ParallelOptions()
                    {
                        MaxDegreeOfParallelism = 5
                    };
                    protectedApiCallHelper.setHeader(url, authenticationResult.AccessToken);
                    await Parallel.ForEachAsync(gdapList, options, async (g, cancellationToken) =>
                    {
                        allgdapRelationList.Add(await PostGdapRelationship(url, g));
                    });

                    
                    Console.WriteLine("Downloading GDAP Relationship(s)...");
                    await exportImportProvider.WriteAsync(allgdapRelationList, $"{Constants.InputFolderPath}/gdapRelationship/gdapRelationship.{Helper.GetExtenstion(type)}");
                    Console.WriteLine($"Downloaded new GDAP Relationship(s) at {Constants.InputFolderPath}/gdapRelationship/gdapRelationship.{Helper.GetExtenstion(type)}");

                }
            }
            catch (IOException ex)
            {
                logger.LogError(ex.Message);
                Console.WriteLine("Make sure all input file(s) are closed before running the operation.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred while save the Granular Relationship");
                logger.LogError(ex.Message);
            }
            return true;
        }

        public async Task<bool> GetAllGDAPAsync(ExportImport type)
        {
            try
            {
                var exportImportProvider = exportImportProviderFactory.Create(type);
                var gdapList = new List<DelegatedAdminRelationship>();
                var authenticationResult = await tokenProvider.GetTokenAsync(Resource.TrafficManager);
                if (authenticationResult != null)
                {
                    Console.WriteLine("Downloading relationship(s)...");
                    string accessToken = authenticationResult.AccessToken;
                    var response = await GetGdapRelationship<JObject>(accessToken);
                    if (response != null)
                    {
                        foreach (JProperty? child in response.Properties().Where(p => !p.Name.StartsWith("@")))
                        {
                            gdapList.AddRange(child.Value.Select(item => item.ToObject<DelegatedAdminRelationship>()));
                        }
                        await exportImportProvider.WriteAsync(gdapList, $"{Constants.OutputFolderPath}/ExistingGdapRelationship.{Helper.GetExtenstion(type)}");
                        Console.WriteLine($"Downloaded existing GDAP relationship(s) at {Constants.OutputFolderPath}/ExistingGdapRelationship.{Helper.GetExtenstion(type)}");
                    }

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
