using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

using MessagePack;

using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;

namespace AspNetCore.SignalR.Hazelcast
{
    public class HazelcastProtocol
    {
        private readonly IReadOnlyList<IHubProtocol> _protocols;

        public HazelcastProtocol(IReadOnlyList<IHubProtocol> protocols)
        {
            _protocols = protocols;
        }

        public byte[] WriteInvocation(string methodName, object[] args) =>
            WriteInvocation(methodName, args, null);

        public byte[] WriteInvocation(string methodName, object[] args, IReadOnlyList<string> excludedConnectionIds)
        {
            var writer = MemoryBufferWriter.Get();

            try
            {
                MessagePackBinary.WriteArrayHeader(writer, 2);

                if (excludedConnectionIds != null && excludedConnectionIds.Count > 0)
                {
                    MessagePackBinary.WriteArrayHeader(writer, excludedConnectionIds.Count);
                    foreach (var id in excludedConnectionIds)
                    {
                        MessagePackBinary.WriteString(writer, id);
                    }
                }
                else
                {
                    MessagePackBinary.WriteArrayHeader(writer, 0);
                }

                WriteSerializedHubMessage(writer, new SerializedHubMessage(new InvocationMessage(methodName, args)));
                return writer.ToArray();
            }
            finally
            {
                MemoryBufferWriter.Return(writer);
            }
        }

        public HazelcastInvocation ReadInvocation(ReadOnlyMemory<byte> data)
        {
            // See WriteInvocation for the format
            ValidateArraySize(ref data, 2, "Invocation");

            // Read excluded Ids
            IReadOnlyList<string> excludedConnectionIds = null;
            var idCount = MessagePackUtil.ReadArrayHeader(ref data);
            if (idCount > 0)
            {
                var ids = new string[idCount];
                for (var i = 0; i < idCount; i++)
                {
                    ids[i] = MessagePackUtil.ReadString(ref data);
                }

                excludedConnectionIds = ids;
            }

            // Read payload
            var message = ReadSerializedHubMessage(ref data);
            return new HazelcastInvocation(message, excludedConnectionIds);
        }

        private void WriteSerializedHubMessage(Stream stream, SerializedHubMessage message)
        {
            // Written as a MessagePack 'map' where the keys are the name of the protocol (as a MessagePack 'str')
            // and the values are the serialized blob (as a MessagePack 'bin').

            MessagePackBinary.WriteMapHeader(stream, _protocols.Count);

            foreach (var protocol in _protocols)
            {
                MessagePackBinary.WriteString(stream, protocol.Name);

                var serialized = message.GetSerializedMessage(protocol);
                var isArray = MemoryMarshal.TryGetArray(serialized, out var array);
                Debug.Assert(isArray);
                MessagePackBinary.WriteBytes(stream, array.Array, array.Offset, array.Count);
            }
        }

        public static SerializedHubMessage ReadSerializedHubMessage(ref ReadOnlyMemory<byte> data)
        {
            var count = MessagePackUtil.ReadMapHeader(ref data);
            var serializations = new SerializedMessage[count];
            for (var i = 0; i < count; i++)
            {
                var protocol = MessagePackUtil.ReadString(ref data);
                var serialized = MessagePackUtil.ReadBytes(ref data);
                serializations[i] = new SerializedMessage(protocol, serialized);
            }

            return new SerializedHubMessage(serializations);
        }

        private static void ValidateArraySize(ref ReadOnlyMemory<byte> data, int expectedLength, string messageType)
        {
            var length = MessagePackUtil.ReadArrayHeader(ref data);

            if (length < expectedLength)
            {
                throw new InvalidDataException($"Insufficient items in {messageType} array.");
            }
        }
    }
}
