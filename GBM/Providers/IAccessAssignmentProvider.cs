using PartnerLed.Model;

namespace PartnerLed.Providers
{
    internal interface IAccessAssignmentProvider
    {
        Task<bool> ExportSecurityGroup(ExportImport type);

        Task<bool> CreateAccessAssignmentRequestAsync(ExportImport type);

        Task<bool> RefreshAccessAssignmentRequest(ExportImport type);
    }
}