using System.IO;
using BlackFox.U2F.Key.messages;

namespace BlackFox.U2F.Codec
{
    public class SerialCodec
    {
        public const byte Version = 0x02;

        public const byte CommandRegister = 0x01;

        public const byte CommandAuthenticate = 0x02;

        /// <exception cref="System.IO.IOException"/>
        /// <exception cref="U2FException"/>
        public static void SendRegisterRequest(Stream outputStream, RegisterRequest registerRequest)
        {
            SendRequest(outputStream, CommandRegister, RawMessageCodec.
                EncodeRegisterRequest(registerRequest));
        }

        /// <exception cref="System.IO.IOException"/>
        /// <exception cref="U2FException"/>
        public static void SendRegisterResponse(Stream outputStream, RegisterResponse registerResponse)
        {
            SendResponse(outputStream, RawMessageCodec.EncodeRegisterResponse(registerResponse));
        }

        /// <exception cref="System.IO.IOException"/>
        /// <exception cref="U2FException"/>
        public static void SendAuthenticateRequest(Stream outputStream, AuthenticateRequest authenticateRequest)
        {
            SendRequest(outputStream, CommandAuthenticate,
                RawMessageCodec.EncodeAuthenticateRequest(authenticateRequest));
        }

        /// <exception cref="System.IO.IOException"/>
        /// <exception cref="U2FException"/>
        public static void SendAuthenticateResponse(Stream outputStream, AuthenticateResponse
            authenticateResponse)
        {
            SendResponse(outputStream, RawMessageCodec.EncodeAuthenticateResponse(authenticateResponse));
        }

        /// <exception cref="U2FException"/>
        /// <exception cref="System.IO.IOException"/>
        static void SendRequest(Stream outputStream, byte command, byte[] encodedBytes)
        {
            if (encodedBytes.Length > 65535)
            {
                throw new U2FException("Message is too long to be transmitted over this protocol");
            }
            using (var dataOutputStream = new BinaryWriter(outputStream))
            {
                dataOutputStream.Write(Version);
                dataOutputStream.Write(command);
                dataOutputStream.Write((short)encodedBytes.Length);
                dataOutputStream.Write(encodedBytes);
            }
        }

        /// <exception cref="U2FException"/>
        /// <exception cref="System.IO.IOException"/>
        static void SendResponse(Stream outputStream, byte[] encodedBytes)
        {
            if (encodedBytes.Length > 65535)
            {
                throw new U2FException("Message is too long to be transmitted over this protocol");
            }
            using (var dataOutputStream = new BinaryWriter(outputStream))
            {
                dataOutputStream.Write((short)encodedBytes.Length);
                dataOutputStream.Write(encodedBytes);
            }
        }

        /// <exception cref="U2FException"/>
        /// <exception cref="System.IO.IOException"/>
        public static IU2FRequest ParseRequest(Stream inputStream)
        {
            using (var dataInputStream = new BinaryReader(inputStream))
            {
                var version = dataInputStream.ReadByte();
                if (version != Version)
                {
                    throw new U2FException($"Unsupported message version: {version}");
                }
                var command = dataInputStream.ReadByte();
                switch (command)
                {
                    case CommandRegister:
                    {
                        return RawMessageCodec.DecodeRegisterRequest(ParseMessage(dataInputStream
                            ));
                    }

                    case CommandAuthenticate:
                    {
                        return RawMessageCodec.DecodeAuthenticateRequest(ParseMessage
                            (dataInputStream));
                    }

                    default:
                    {
                        throw new U2FException($"Unsupported command: {command}");
                    }
                }
            }
        }

        /// <exception cref="U2FException"/>
        /// <exception cref="System.IO.IOException"/>
        public static RegisterResponse ParseRegisterResponse(Stream inputStream)
        {
            using (var dataInputStream = new BinaryReader(inputStream))
            {
                return RawMessageCodec.DecodeRegisterResponse(ParseMessage(dataInputStream));
            }
        }

        /// <exception cref="U2FException"/>
        /// <exception cref="System.IO.IOException"/>
        public static AuthenticateResponse ParseAuthenticateResponse(Stream inputStream)
        {
            using (var dataInputStream = new BinaryReader(inputStream))
            {
                return RawMessageCodec.DecodeAuthenticateResponse(ParseMessage(dataInputStream));
            }
        }

        /// <exception cref="System.IO.IOException"/>
        static byte[] ParseMessage(BinaryReader dataInputStream)
        {
            var size = dataInputStream.ReadUInt16();
            return dataInputStream.ReadBytes(size);
        }
    }
}
