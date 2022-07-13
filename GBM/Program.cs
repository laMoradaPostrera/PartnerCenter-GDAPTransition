using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PartnerLed;
using PartnerLed.Logger;
using PartnerLed.Model;
using PartnerLed.Providers;
using PartnerLed.Utility;




var AppSetting = new AppSetting();

using IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((services) =>
    {
        services.AddSingleton(AppSetting);
        services.AddSingleton<IExportImportProviderFactory, ExportImportProviderFactory>();
        services.AddSingleton<ITokenProvider, TokenProvider>();
        services.AddSingleton<IDapProvider, DapProvider>();
        services.AddSingleton<IAzureRoleProvider, AzureRoleProvider>();
        services.AddSingleton<IGdapProvider, GdapProvider>();
        services.AddSingleton<IAccessAssignmentProvider, AccessAssignmentProvider>();
    }).ConfigureLogging(logging =>
    {
        logging.ClearProviders().AddCustomLogger();
    }).Build();


await RunAsync(host.Services);
await host.RunAsync();

static async Task RunAsync(IServiceProvider serviceProvider)
{
    setupDirectory();
    var type = setupFormat();

    Console.WriteLine("Please choose an option..");

    DisplayOptions();

SelectOption:
    Console.Write('>');
    var option = Console.ReadLine();
    if (!short.TryParse(option, out short input) || !(input >= 1 && input <= 10))
    {
        Console.WriteLine("Invalid input, Please try again.");
        DisplayOptions();

        goto SelectOption;
    }

    Stopwatch stopwatch = Stopwatch.StartNew();

    var result = input switch
    {
        1 => await serviceProvider.GetRequiredService<IDapProvider>().ExportCustomerDetails(type),
        2 => await serviceProvider.GetRequiredService<IDapProvider>().ExportCustomerBulk(),
        3 => await serviceProvider.GetRequiredService<IAzureRoleProvider>().ExportAzureDirectoryRoles(type),
        4 => await serviceProvider.GetRequiredService<IAccessAssignmentProvider>().ExportSecurityGroup(type),
        5 => await serviceProvider.GetRequiredService<IGdapProvider>().GetAllGDAPAsync(type),
        6 => await serviceProvider.GetRequiredService<IDapProvider>().GenerateDAPRelatioshipwithAccessAssignment(type),
        7 => await serviceProvider.GetRequiredService<IGdapProvider>().CreateGDAPRequestAsync(type),
        8 => await serviceProvider.GetRequiredService<IGdapProvider>().RefreshGDAPRequestAsync(type),
        9 => await serviceProvider.GetRequiredService<IAccessAssignmentProvider>().CreateAccessAssignmentRequestAsync(type),
        10 => await serviceProvider.GetRequiredService<IAccessAssignmentProvider>().RefreshAccessAssignmentRequest(type),
        _ => throw new InvalidOperationException("Invalid input")
    };

    stopwatch.Stop();
    Console.WriteLine($"[Completed the operation in {stopwatch.Elapsed}]\n");
    goto SelectOption;
}

static void DisplayOptions()
{
    Console.WriteLine("\nDownload Operations: ");
    Console.WriteLine("\t 1. Download eligible customers list");
    Console.WriteLine("\t 2. Download eligible customers for very large list (compressed format)");
    Console.WriteLine("\t 3. Download Azure AD Roles");
    Console.WriteLine("\t 4. Download Partner Tenant's Security Group(s)");
    Console.WriteLine("\t 5. Download existing GDAP relationship(s)\n");
    Console.WriteLine("GDAP Relationship Operations: ");
    Console.WriteLine("\t 6. One flow generation");
    Console.WriteLine("\t 7. Create GDAP Relationship(s)");
    Console.WriteLine("\t 8. Refresh GDAP Relationship status\n");
    Console.WriteLine("Provision Security Group Operations: ");
    Console.WriteLine("\t 9. Create Security Group-Role Assignment(s)");
    Console.WriteLine("\t 10. Refresh Security Group-Role Assignment status");
}

static void setupDirectory()
{
    Environment.SetEnvironmentVariable(Constants.BasepathVariable, Directory.GetParent(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)).Parent.Parent.FullName);
    Directory.CreateDirectory($"{Constants.InputFolderPath}/gdapRelationship");
    Directory.CreateDirectory($"{Constants.InputFolderPath}/accessAssignment");
    Directory.CreateDirectory(Constants.OutputFolderPath);
    Directory.CreateDirectory(Constants.LogFolderPath);
}

static ExportImport setupFormat()
{
    Console.WriteLine("\n\nPlease choose an file type you like to work with for this tool");
    Console.WriteLine("1. CSV");
    Console.WriteLine("2. JSON");
    Console.Write('>');

SelectOption:
    var option = Console.ReadLine();
    if (!short.TryParse(option, out short input) || !(input >= 1 && input <= 2))
    {
        Console.WriteLine("Invalid input, Please try again, possible values are {1, 2}");
        goto SelectOption;
    }
    Console.Clear();
    Console.WriteLine("GDAP Bulk Migration Tool.");
    return input == 1 ? ExportImport.Csv : ExportImport.Json;
}
