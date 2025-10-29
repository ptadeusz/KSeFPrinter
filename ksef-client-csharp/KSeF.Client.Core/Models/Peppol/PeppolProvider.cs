namespace KSeF.Client.Core.Models.Peppol
{
    public class PeppolProvider
    {
        /// <summary>
        /// Identyfikator dostawcy (np. kod w rejestrze Peppol).
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Nazwa dostawcy usług Peppol.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Adres URL lub endpoint dostawcy.
        /// </summary>
        public string Endpoint { get; set; }
    }
}
