using PartnerLed.Model;

namespace PartnerLed.Providers
{
    public interface IDapProvider
    {
        Task<bool> ExportCustomerDetails(ExportImport type);

        Task<bool> ExportCustomerBulk();

        Task<bool> GenerateDAPRelatioshipwithAccessAssignment(ExportImport type);
    }
}