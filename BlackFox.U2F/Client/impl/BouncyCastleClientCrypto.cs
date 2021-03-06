using System.Text;
using Org.BouncyCastle.Security;

namespace BlackFox.U2F.Client.impl
{
    public class BouncyCastleClientCrypto : IClientCrypto
	{
	    public static BouncyCastleClientCrypto Instance { get; } = new BouncyCastleClientCrypto();

	    private BouncyCastleClientCrypto()
	    {
	    }

	    public byte[] ComputeSha256(string message)
		{
            try
			{
                var bytes = Encoding.UTF8.GetBytes(message);
                return DigestUtilities.CalculateDigest("SHA-256", bytes);
			}
			catch (SecurityUtilityException e)
			{
				throw new U2FException("Cannot compute SHA-256", e);
			}
		}
	}
}
