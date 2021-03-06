using System.Collections.Generic;
using BlackFox.U2F.Server.data;
using BlackFox.U2F.Server.messages;

namespace BlackFox.U2F.Server
{
	public interface IU2FServer
	{
		// registration //
		/// <exception cref="U2FException"/>
		RegisterRequest GetRegistrationRequest(string accountName, string appId);

		/// <exception cref="U2FException"/>
		SecurityKeyData ProcessRegistrationResponse(RegisterResponse registerResponse, long currentTimeInMillis);

		// authentication //
		/// <exception cref="U2FException"/>
		System.Collections.Generic.IList<SignRequest> GetSignRequests(string accountName, string appId);

		/// <exception cref="U2FException"/>
		SecurityKeyData ProcessSignResponse(SignResponse signResponse);

		// token management //
		IList<SecurityKeyData> GetAllSecurityKeys(string accountName);

		/// <exception cref="U2FException"/>
		void RemoveSecurityKey(string accountName, byte[] publicKey);
	}
}
