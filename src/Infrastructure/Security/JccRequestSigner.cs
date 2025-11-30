using System.Security.Cryptography;
using System.Text;

namespace Payments.Infrastructure.Security;

public interface IJccRequestSigner
{
    (string xHash, string xSignature) Sign(string body);
}

/// Signs request bodies per JCC "Calculating hash and signature".
/// Private key must be the same one whose public cert you uploaded to JCC portal.
public class JccRequestSigner : IJccRequestSigner
{
    private readonly RSA _privateKey;

    public JccRequestSigner(string privateKeyPem)
    {
        _privateKey = RSA.Create();
        _privateKey.ImportFromPem(privateKeyPem);
    }

    public (string xHash, string xSignature) Sign(string body)
    {
        // 1) SHA-256 hash (base64)
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(body));
        var xHash = Convert.ToBase64String(hashBytes);

        // 2) RSA-SHA256 signature
        var signatureBytes = _privateKey.SignData(
            Encoding.UTF8.GetBytes(body),
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        var xSignature = Convert.ToBase64String(signatureBytes);

        return (xHash, xSignature);
    }
}
