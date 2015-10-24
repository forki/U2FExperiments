﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BlackFox.U2F.Gnubby;
using BlackFox.U2F.Key.messages;
using Common.Logging;
using JetBrains.Annotations;

namespace BlackFox.U2F.GnubbyApi
{
    class Authentifier
    {
        static readonly TimeSpan timeBetweenRegisterCalls = TimeSpan.FromMilliseconds(200);
        static readonly TimeSpan timeBetweenOpenCalls = TimeSpan.FromMilliseconds(200);
        class KeyBusyException : Exception
        {

        }

        static readonly ILog log = LogManager.GetLogger(typeof(Authentifier));

        readonly IKeyId keyId;
        readonly ICollection<RegisterRequest> requests;

        public Authentifier([NotNull] IKeyId keyId, [NotNull] ICollection<RegisterRequest> requests)
        {
            if (keyId == null)
            {
                throw new ArgumentNullException(nameof(keyId));
            }
            if (requests == null)
            {
                throw new ArgumentNullException(nameof(requests));
            }

            this.keyId = keyId;
            this.requests = requests;
        }

        public async Task<AuthentifierResult> AuthenticateAsync(CancellationToken cancellationToken)
        {
            using (var key = await OpenKeyAsync(cancellationToken))
            {
                while (true)
                {
                    var signOnceResult = await TrySigningOnceAsync(key, cancellationToken);

                    if (signOnceResult.HasValue)
                    {
                        return signOnceResult.Value;
                    }

                    await TaskEx.Delay(timeBetweenRegisterCalls, cancellationToken);
                }
            }
        }

        async Task<AuthentifierResult?> TrySigningOnceAsync(IKey key, CancellationToken cancellationToken)
        {
            int challengesTried = 0;
            foreach (var request in requests)
            {
                challengesTried += 1;

                var requestSignResult = await TrySignOneRequest(key, request, cancellationToken);
                if (requestSignResult.HasValue)
                {
                    return requestSignResult.Value;
                }
            }
            if (challengesTried == 0)
            {
                return AuthentifierResult.Failure(KeyResponseStatus.Failure);
            }

            return null;
        }

        async Task<AuthentifierResult?> TrySignOneRequest(IKey key, RegisterRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await key.RegisterAsync(request, cancellationToken);

                log.Info(result.Status.ToString());
                switch (result.Status)
                {
                    case KeyResponseStatus.Success:
                        return AuthentifierResult.Success(request, result.Data);
                }
            }
            catch (KeyGoneException)
            {
                // No sense in continuing with this signer, the key isn't physically present anymore
                log.DebugFormat("Key '{0}' is gone", keyId);
                throw;
            }
            catch (TaskCanceledException)
            {
                // Let cancellation bubble up
                throw;
            }
            catch (KeyBusyException)
            {
                // Maybe it won't be busy later
            }
            catch (Exception exception)
            {
                log.Error("Authenticate request failed", exception);
            }
            return null;
        }

        private async Task<IKey> OpenKeyAsync(CancellationToken cancellationToken)
        {
            IKey key;
            do
            {
                try
                {
                    key = await keyId.OpenAsync(cancellationToken);
                }
                catch (KeyBusyException)
                {
                    key = null;
                    await TaskEx.Delay(timeBetweenOpenCalls, cancellationToken);
                }
            } while (key == null);
            return key;
        }
    }
}