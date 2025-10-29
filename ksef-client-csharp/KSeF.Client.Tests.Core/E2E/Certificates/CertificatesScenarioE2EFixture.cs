using KSeF.Client.Core.Models.Certificates;

namespace KSeF.Client.Tests.Core.E2E.Certificates;

public class CertificatesScenarioE2EFixture
{
    public string AccessToken { get; set; }
    public CertificateLimitResponse Limits { get; set; }
    public CertificateEnrollmentsInfoResponse EnrollmentInfo { get; set; }
    public string EnrollmentReference { get; set; }
    public CertificateEnrollmentStatusResponse EnrollmentStatus { get; set; }
    public List<string> SerialNumbers { get; set; }
    public CertificateListResponse RetrievedCertificates { get; set; }
    public CertificateMetadataListResponse MetadataList { get; set; }
}