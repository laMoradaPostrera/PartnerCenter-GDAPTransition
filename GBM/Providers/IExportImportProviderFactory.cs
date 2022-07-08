using PartnerLed.Model;

namespace PartnerLed.Providers
{
    internal interface IExportImportProviderFactory
    {
        IExportImportProvider Create(ExportImport type);
    }
}