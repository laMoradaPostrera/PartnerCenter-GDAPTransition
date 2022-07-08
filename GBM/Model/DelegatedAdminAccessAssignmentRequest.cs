namespace PartnerLed.Model
{
    /// <summary>
    /// 
    /// </summary>
    internal class DelegatedAdminAccessAssignmentRequest
    {
        /// <summary>
        /// Gets or sets the ID of the Gdaplationship
        /// </summary>
        public string GdapRelationshipId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the access assignment.
        /// </summary>
        public string AccessAssignmentId { get; set; }

        /// <summary>
        /// Gets or sets the status of the assignment.
        /// </summary>
        public string Status { get; set; }


    }
}
