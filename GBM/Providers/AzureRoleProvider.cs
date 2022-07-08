using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PartnerLed.Model;
using PartnerLed.Utility;
using System.Reflection;

namespace PartnerLed.Providers
{

    internal class AzureRoleProvider : IAzureRoleProvider
    {

        /// <summary>
        /// Provider Instances.
        /// </summary>
        private readonly IExportImportProviderFactory exportImportProviderFactory;

        /// <summary>
        /// AzureRole provider constructor.
        /// </summary>
        public AzureRoleProvider(IExportImportProviderFactory exportImportProviderFactory)
        {
            this.exportImportProviderFactory = exportImportProviderFactory;

        }

        public async Task<bool> ExportAzureDirectoryRoles(ExportImport type)
        {
            var jsonReader = exportImportProviderFactory.Create(ExportImport.Json);
            string? path = $"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}/Configuration/ADRoles.json";
            try
            {
                using (StreamReader r = new StreamReader(path))
                {
                    string json = r.ReadToEnd();
                    Save(JsonConvert.DeserializeObject<JArray>(json), type);
                }
            }
            catch
            {
                Console.WriteLine($"Error occurred while reading the AD roles.");
                throw;
            }

            return await Task.FromResult(true);
        }

        /// <summary>
        /// Display the result of the Web API call
        /// </summary>
        /// <param name="result">Object to save as CSV file</param>
        private async void Save(JArray? result, ExportImport type)
        {
            var exportImportProvider = exportImportProviderFactory.Create(type);
            var data = new List<ADRole>();
            try
            {
                foreach (var child in result)
                {
                    data.AddRange(child["roles"].Value<JArray>().ToObject<List<ADRole>>());
                }

                var path = $"{Constants.OutputFolderPath}/ADRoles";
                await exportImportProvider.WriteAsync(data, $"{path}.{Helper.GetExtenstion(type)}");
                Console.WriteLine($"Exported Azure Directory Roles at {Constants.OutputFolderPath}/ADRoles.{Helper.GetExtenstion(type)}");

            }
            catch
            {
                Console.WriteLine($"Error occurred while save the AD roles");
                throw;
            }
        }

    }


}
