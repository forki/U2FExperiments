using System.Collections.Generic;
using System.Linq;
using BlackFox.U2F.Server;
using BlackFox.U2F.Server.data;
using BlackFox.U2F.Server.impl;
using BlackFox.U2F.Server.messages;
using Moq;
using NFluent;
using NUnit.Framework;
using Org.BouncyCastle.X509;
using static BlackFox.U2F.Tests.TestVectors;

namespace BlackFox.U2F.Tests.Server
{
    public class U2FServerReferenceImplTest
    {
        private IServerCrypto crypto;
        private Mock<IChallengeGenerator> mockChallengeGenerator;
        private Mock<IServerDataStore> mockDataStore;
        private Mock<ISessionIdGenerator> mockSessionIdGenerator;

        [SetUp]
        public virtual void Setup()
        {
            mockChallengeGenerator = new Mock<IChallengeGenerator>();
            mockSessionIdGenerator = new Mock<ISessionIdGenerator>();
            mockDataStore = new Mock<IServerDataStore>();
            crypto = new BouncyCastleServerCrypto();

            var trustedCertificates = new List<X509Certificate> {VENDOR_CERTIFICATE};

            mockChallengeGenerator.Setup(x => x.GenerateChallenge(ACCOUNT_NAME)).Returns(SERVER_CHALLENGE_ENROLL);
            mockSessionIdGenerator.Setup(x => x.GenerateSessionId(ACCOUNT_NAME)).Returns(SESSION_ID);
            mockDataStore.Setup(x => x.StoreSessionData(It.IsAny<EnrollSessionData>())).Returns(SESSION_ID);
            mockDataStore.Setup(x => x.GetTrustedCertificates()).Returns(trustedCertificates);
            mockDataStore.Setup(x => x.GetSecurityKeyData(ACCOUNT_NAME))
                .Returns(
                    new[] {new SecurityKeyData(0L, KEY_HANDLE, USER_PUBLIC_KEY_SIGN_HEX, VENDOR_CERTIFICATE, 0)}.ToList());
        }

        [Test]
        public virtual void TestSanitizeOrigin()
        {
            Assert.AreEqual("http://example.com", U2FServerReferenceImpl
                .CanonicalizeOrigin("http://example.com"));
            Assert.AreEqual("http://example.com", U2FServerReferenceImpl
                .CanonicalizeOrigin("http://example.com/"));
            Assert.AreEqual("http://example.com", U2FServerReferenceImpl
                .CanonicalizeOrigin("http://example.com/foo"));
            Assert.AreEqual("http://example.com", U2FServerReferenceImpl
                .CanonicalizeOrigin("http://example.com/foo?bar=b"));
            Assert.AreEqual("http://example.com", U2FServerReferenceImpl
                .CanonicalizeOrigin("http://example.com/foo#fragment"));
            Assert.AreEqual("https://example.com", U2FServerReferenceImpl
                .CanonicalizeOrigin("https://example.com"));
            Assert.AreEqual("https://example.com", U2FServerReferenceImpl
                .CanonicalizeOrigin("https://example.com/foo"));
        }

        [Test]
        public virtual void TestGetRegistrationRequest()
        {
            var u2FServer = new U2FServerReferenceImpl(mockChallengeGenerator.Object, mockDataStore.Object, crypto,
                TRUSTED_DOMAINS);

            var registrationRequest = u2FServer.GetRegistrationRequest(ACCOUNT_NAME, APP_ID_ENROLL);

            var expected = new RegisterRequest("U2F_V2", SERVER_CHALLENGE_ENROLL_BASE64, APP_ID_ENROLL, SESSION_ID);
            Check.That(registrationRequest).IsEqualTo(expected);
        }

        [Test]
        public virtual void TestProcessRegistrationResponse_NoTransports()
        {
            mockDataStore.Setup(x => x.GetEnrollSessionData(SESSION_ID))
                .Returns(new EnrollSessionData(ACCOUNT_NAME, APP_ID_ENROLL, SERVER_CHALLENGE_ENROLL));
            var u2FServer = new U2FServerReferenceImpl(mockChallengeGenerator.Object, mockDataStore.Object, crypto,
                TRUSTED_DOMAINS);

            var registrationResponse = new RegisterResponse(REGISTRATION_DATA_BASE64, BROWSER_DATA_ENROLL_BASE64,
                SESSION_ID);
            u2FServer.ProcessRegistrationResponse(registrationResponse, 0L);

            var expectedKeyData = new SecurityKeyData(0L, KEY_HANDLE, USER_PUBLIC_KEY_ENROLL_HEX, VENDOR_CERTIFICATE, 0);
            mockDataStore.Verify(x => x.AddSecurityKeyData(ACCOUNT_NAME, expectedKeyData));
        }

        [Test]
        public virtual void TestProcessRegistrationResponse_OneTransport()
        {
            mockDataStore.Setup(x => x.GetEnrollSessionData(SESSION_ID))
                .Returns(new EnrollSessionData(ACCOUNT_NAME, APP_ID_ENROLL, SERVER_CHALLENGE_ENROLL));
            var trustedCertificates = new List<X509Certificate>();
            trustedCertificates.Add(TRUSTED_CERTIFICATE_ONE_TRANSPORT);
            mockDataStore.Setup(x => x.GetTrustedCertificates()).Returns(trustedCertificates);
            var u2FServer = new U2FServerReferenceImpl(mockChallengeGenerator.Object, mockDataStore.Object, crypto,
                TRUSTED_DOMAINS);

            var registrationResponse = new RegisterResponse(REGISTRATION_RESPONSE_DATA_ONE_TRANSPORT_BASE64,
                BROWSER_DATA_ENROLL_BASE64, SESSION_ID);
            u2FServer.ProcessRegistrationResponse(registrationResponse, 0L);

            var transports = new List<SecurityKeyDataTransports>();
            transports.Add(SecurityKeyDataTransports.BluetoothRadio);
            var expectedKeyData = new SecurityKeyData(0L, transports, KEY_HANDLE, USER_PUBLIC_KEY_ENROLL_HEX,
                TRUSTED_CERTIFICATE_ONE_TRANSPORT, 0);
            mockDataStore.Verify(x => x.AddSecurityKeyData(ACCOUNT_NAME, expectedKeyData));
        }

        [Test]
        public virtual void TestProcessRegistrationResponse_MultipleTransports()
        {
            mockDataStore.Setup(x => x.GetEnrollSessionData(SESSION_ID))
                .Returns(new EnrollSessionData(ACCOUNT_NAME, APP_ID_ENROLL, SERVER_CHALLENGE_ENROLL));
            var trustedCertificates = new List<X509Certificate>();
            trustedCertificates.Add(TRUSTED_CERTIFICATE_MULTIPLE_TRANSPORTS);
            mockDataStore.Setup(x => x.GetTrustedCertificates()).Returns(trustedCertificates);
            var u2FServer = new U2FServerReferenceImpl(mockChallengeGenerator.Object, mockDataStore.Object, crypto,
                TRUSTED_DOMAINS);

            var registrationResponse = new RegisterResponse(REGISTRATION_RESPONSE_DATA_MULTIPLE_TRANSPORTS_BASE64,
                BROWSER_DATA_ENROLL_BASE64, SESSION_ID);
            u2FServer.ProcessRegistrationResponse(registrationResponse, 0L);

            var transports = new List<SecurityKeyDataTransports>();
            transports.Add(SecurityKeyDataTransports.BluetoothRadio);
            transports.Add(SecurityKeyDataTransports.BluetoothLowEnergy);
            transports.Add(SecurityKeyDataTransports.Nfc);
            var expectedKeyData = new SecurityKeyData(0L, transports, KEY_HANDLE, USER_PUBLIC_KEY_ENROLL_HEX,
                TRUSTED_CERTIFICATE_MULTIPLE_TRANSPORTS, 0);
            mockDataStore.Verify(x => x.AddSecurityKeyData(ACCOUNT_NAME, expectedKeyData));
        }

        [Test]
        public virtual void TestProcessRegistrationResponse_MalformedTransports()
        {
            mockDataStore.Setup(x => x.GetEnrollSessionData(SESSION_ID))
                .Returns(new EnrollSessionData(ACCOUNT_NAME, APP_ID_ENROLL, SERVER_CHALLENGE_ENROLL));
            var trustedCertificates = new List<X509Certificate>();
            trustedCertificates.Add(TRUSTED_CERTIFICATE_MALFORMED_TRANSPORTS_EXTENSION);
            mockDataStore.Setup(x => x.GetTrustedCertificates()).Returns(trustedCertificates);
            var u2FServer = new U2FServerReferenceImpl(mockChallengeGenerator.Object, mockDataStore.Object, crypto,
                TRUSTED_DOMAINS);

            var registrationResponse = new RegisterResponse(REGISTRATION_RESPONSE_DATA_MALFORMED_TRANSPORTS_BASE64,
                BROWSER_DATA_ENROLL_BASE64, SESSION_ID);
            u2FServer.ProcessRegistrationResponse(registrationResponse, 0L);

            var expectedKeyData = new SecurityKeyData(0L, null, KEY_HANDLE, USER_PUBLIC_KEY_ENROLL_HEX,
                TRUSTED_CERTIFICATE_MALFORMED_TRANSPORTS_EXTENSION, 0);
            mockDataStore.Verify(x => x.AddSecurityKeyData(ACCOUNT_NAME, expectedKeyData));
        }


        // transports 
        [Test]
        public virtual void TestProcessRegistrationResponse2()
        {
            mockDataStore.Setup(x => x.GetEnrollSessionData(SESSION_ID))
                .Returns(new EnrollSessionData(ACCOUNT_NAME, APP_ID_ENROLL, SERVER_CHALLENGE_ENROLL));
            var trustedCertificates = new List<X509Certificate>();
            trustedCertificates.Add(VENDOR_CERTIFICATE);
            trustedCertificates.Add(TRUSTED_CERTIFICATE_2);
            mockDataStore.Setup(x => x.GetTrustedCertificates()).Returns(trustedCertificates);
            var u2FServer = new U2FServerReferenceImpl(mockChallengeGenerator.Object, mockDataStore.Object, crypto,
                TRUSTED_DOMAINS);
            var registrationResponse = new RegisterResponse(REGISTRATION_DATA_2_BASE64, BROWSER_DATA_2_BASE64,
                SESSION_ID);
            u2FServer.ProcessRegistrationResponse(registrationResponse, 0L);
            var expectedKeyData = new SecurityKeyData(0L, null, KEY_HANDLE_2, USER_PUBLIC_KEY_2, TRUSTED_CERTIFICATE_2,
                0);
            mockDataStore.Verify(x => x.AddSecurityKeyData(ACCOUNT_NAME, expectedKeyData));
        }


        // transports
        [Test]
        public virtual void TestGetSignRequest()
        {
            var u2FServer = new U2FServerReferenceImpl(mockChallengeGenerator.Object, mockDataStore.Object, crypto,
                TRUSTED_DOMAINS);
            mockChallengeGenerator.Setup(x => x.GenerateChallenge(ACCOUNT_NAME))
                .Returns(SERVER_CHALLENGE_SIGN);

            var signRequest = u2FServer.GetSignRequests(ACCOUNT_NAME, APP_ID_SIGN);

            var expected = new SignRequest("U2F_V2", SERVER_CHALLENGE_SIGN_BASE64, APP_ID_SIGN, KEY_HANDLE_BASE64,
                SESSION_ID);
            Assert.AreEqual(expected, signRequest[0]);
        }

        /// <exception cref="U2FException" />
        [Test]
        public virtual void TestProcessSignResponse()
        {
            mockDataStore.Setup(x => x.GetSignSessionData(SESSION_ID))
                .Returns(new SignSessionData(ACCOUNT_NAME, APP_ID_SIGN, SERVER_CHALLENGE_SIGN, USER_PUBLIC_KEY_SIGN_HEX));
            var u2FServer = new U2FServerReferenceImpl(mockChallengeGenerator.Object, mockDataStore.Object, crypto,
                TRUSTED_DOMAINS);

            var signResponse = new SignResponse(BROWSER_DATA_SIGN_BASE64, SIGN_RESPONSE_DATA_BASE64,
                SERVER_CHALLENGE_SIGN_BASE64, SESSION_ID, APP_ID_SIGN);

            u2FServer.ProcessSignResponse(signResponse);
        }

        
		/// <exception cref="U2FException"/>
		[Test]
		public virtual void TestProcessSignResponseBadOrigin()
		{
            mockDataStore.Setup(x => x.GetSignSessionData(SESSION_ID))
                .Returns(new SignSessionData(ACCOUNT_NAME, APP_ID_SIGN, SERVER_CHALLENGE_SIGN, USER_PUBLIC_KEY_SIGN_HEX));

            var u2FServer = new U2FServerReferenceImpl(mockChallengeGenerator.Object
				, mockDataStore.Object, crypto, new List<string> { "http://some-other-domain.com"});
			var signResponse = new SignResponse(BROWSER_DATA_SIGN_BASE64, SIGN_RESPONSE_DATA_BASE64, SERVER_CHALLENGE_SIGN_BASE64, SESSION_ID, APP_ID_SIGN);

			try
			{
				u2FServer.ProcessSignResponse(signResponse);
				Assert.Fail("expected exception, but didn't get it");
			}
			catch (U2FException e)
			{
				Assert.IsTrue(e.Message.Contains("is not a recognized home origin"));
			}
		}

		[Test]
        [Ignore("TODO: put test back in once we have signature sample on a correct browserdata json")]
        // (currently, this test uses an enrollment browserdata during a signature)
        public virtual void TestProcessSignResponse2()
		{
		    mockDataStore.Setup(x => x.GetSignSessionData(SESSION_ID))
		        .Returns(new SignSessionData(ACCOUNT_NAME, APP_ID_2, SERVER_CHALLENGE_SIGN, USER_PUBLIC_KEY_2));

		    mockDataStore.Setup(x => x.GetSecurityKeyData(ACCOUNT_NAME))
		        .Returns(new List<SecurityKeyData>
		        {
		            new SecurityKeyData(0L, KEY_HANDLE_2, USER_PUBLIC_KEY_2, VENDOR_CERTIFICATE, 0)
		        });
            var u2FServer = new U2FServerReferenceImpl(mockChallengeGenerator.Object, mockDataStore.Object, crypto, TRUSTED_DOMAINS);
			var signResponse = new SignResponse(BROWSER_DATA_2_BASE64, SIGN_DATA_2_BASE64, CHALLENGE_2_BASE64, SESSION_ID, APP_ID_2);
			u2FServer.ProcessSignResponse(signResponse);
		}
    }
}