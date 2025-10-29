using KSeF.Client.Core.Models.Certificates;

namespace KSeF.Client.Api.Builders.Certificates;

public interface ISendCertificateEnrollmentRequestBuilder
{
    ISendCertificateEnrollmentRequestBuilderWithCertificateName WithCertificateName(string certificateName);
}

public interface ISendCertificateEnrollmentRequestBuilderWithCertificateName
{
    ISendCertificateEnrollmentRequestBuilderWithCsr WithCsr(string csr);
    ISendCertificateEnrollmentRequestBuilderWithCertificateType WithCertificateType(CertificateType certificateType);
}

public interface ISendCertificateEnrollmentRequestBuilderWithCertificateType
{
    ISendCertificateEnrollmentRequestBuilderWithCsr WithCsr(string csr);
}

public interface ISendCertificateEnrollmentRequestBuilderWithCsr
{
    ISendCertificateEnrollmentRequestBuilderWithCsr WithValidFrom(DateTimeOffset validFrom);
    SendCertificateEnrollmentRequest Build();
}

internal class SendCertificateEnrollmentRequestBuilderImpl :
    ISendCertificateEnrollmentRequestBuilder,
    ISendCertificateEnrollmentRequestBuilderWithCertificateName,
    ISendCertificateEnrollmentRequestBuilderWithCertificateType,
    ISendCertificateEnrollmentRequestBuilderWithCsr
{
    private string _certificateName;
    private string _csr;
    private DateTimeOffset? _validFrom;
    private CertificateType _certificateType;
    private bool _certificateTypeSet = false;

    public static ISendCertificateEnrollmentRequestBuilder Create() =>
        new SendCertificateEnrollmentRequestBuilderImpl();

    public ISendCertificateEnrollmentRequestBuilderWithCertificateName WithCertificateName(string certificateName)
    {
        if (string.IsNullOrWhiteSpace(certificateName))
            throw new ArgumentException("CertificateName cannot be null or empty.");

        _certificateName = certificateName;
        return this;
    }

    public ISendCertificateEnrollmentRequestBuilderWithCertificateType WithCertificateType(CertificateType certificateType)
    {
        _certificateType = certificateType;
        _certificateTypeSet = true;
        return this;
    }

    public ISendCertificateEnrollmentRequestBuilderWithCsr WithCsr(string csr)
    {
        if (string.IsNullOrWhiteSpace(csr))
            throw new ArgumentException("CSR cannot be null or empty.");

        _csr = csr;
        return this;
    }

    public ISendCertificateEnrollmentRequestBuilderWithCsr WithValidFrom(DateTimeOffset validFrom)
    {
        _validFrom = validFrom;
        return this;
    }

    public SendCertificateEnrollmentRequest Build()
    {
        if (string.IsNullOrWhiteSpace(_certificateName))
            throw new InvalidOperationException("CertificateName is required.");
            
        if (!_certificateTypeSet)
            throw new InvalidOperationException("CertificateType is required.");
        if (string.IsNullOrWhiteSpace(_csr))
            throw new InvalidOperationException("CSR is required.");

        return new SendCertificateEnrollmentRequest
        {
            CertificateName = _certificateName,
            CertificateType = _certificateType,
            Csr = _csr,
            ValidFrom = _validFrom
        };
    }
}

public static class SendCertificateEnrollmentRequestBuilder
{
    public static ISendCertificateEnrollmentRequestBuilder Create() =>
        SendCertificateEnrollmentRequestBuilderImpl.Create();
}