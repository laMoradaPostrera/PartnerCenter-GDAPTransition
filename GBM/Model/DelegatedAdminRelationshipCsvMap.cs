using CsvHelper.Configuration;

namespace PartnerLed.Model
{

    public class DelegatedAdminRelationshipMap : ClassMap<DelegatedAdminRelationship>
    {
        public DelegatedAdminRelationshipMap()
        {
            Map(d => d.Customer.DisplayName ).Name("Customer.DisplayName");
            Map(d => d.Customer.TenantId).Name("Customer.TenantId");
        }

    }
}

