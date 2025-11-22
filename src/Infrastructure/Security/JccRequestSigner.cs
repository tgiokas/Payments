using System.Security.Cryptography;
using System.Text;

namespace Payments.Infrastructure.Security;

public interface IJccRequestSigner
{
    (string XHash, string XSignature) Sign(string body);
}

/// <summary>
/// Signs request bodies per JCC "Calculating hash and signature".
/// Private key must be the same one whose public cert you uploaded to JCC portal.
/// </summary>
public sealed class JccRequestSigner : IJccRequestSigner
{
    private readonly RSA _rsa;

    public JccRequestSigner(string privateKeyPem)
    {
        _rsa = RSA.Create();
        _rsa.ImportFromPem(privateKeyPem);
    }

    public (string XHash, string XSignature) Sign(string body)
    {
        var bodyBytes = Encoding.UTF8.GetBytes(body);

        // 1) SHA256 hash raw bytes
        var hashBytes = SHA256.HashData(bodyBytes);

        // 2) Base64 hash => X-Hash
        var xHash = Convert.ToBase64String(hashBytes);

        // 3) RSA sign the raw hash bytes with SHA256withRSA
        var sigBytes = _rsa.SignHash(hashBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        // 4) Base64 signature => X-Signature
        var xSig = Convert.ToBase64String(sigBytes);

        return (xHash, xSig);
    }
}
