using System.Collections.Generic;
using BlackFox.U2F.Server.data;
using Org.BouncyCastle.X509;

namespace BlackFox.U2F.Server
{
	public interface IDataStore
	{
		// attestation certs and trust
		void AddTrustedCertificate(X509Certificate certificate);

		HashSet<X509Certificate> GetTrustedCertificates ();

		// session handling
		string StoreSessionData(EnrollSessionData sessionData);

		/* sessionId */
		SignSessionData GetSignSessionData(string sessionId);

		EnrollSessionData GetEnrollSessionData(string sessionId);

		// security key management
		void AddSecurityKeyData(string accountName, SecurityKeyData securityKeyData);

		IList<SecurityKeyData> GetSecurityKeyData(string accountName);

		void RemoveSecuityKey(string accountName, byte[] publicKey);

		void UpdateSecurityKeyCounter(string accountName, byte[] publicKey, int newCounterValue);
	}
}
