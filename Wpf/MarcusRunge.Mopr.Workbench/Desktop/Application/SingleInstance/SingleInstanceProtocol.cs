using System;
using System.Buffers.Binary;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WorkbenchResources = MarcusRunge.Mopr.Workbench.Properties.Resources;

namespace MarcusRunge.Mopr.Workbench.Application.SingleInstance
{
    internal static class SingleInstanceProtocol
    {
        private const int MaximumMessageLength = 1024 * 1024;

        public static async Task WriteAsync<T>(Stream stream, T value, CancellationToken cancellationToken)
        {
            var payload = JsonSerializer.SerializeToUtf8Bytes(value);
            var header = new byte[sizeof(int)];
            BinaryPrimitives.WriteInt32LittleEndian(header, payload.Length);

            await stream.WriteAsync(header, cancellationToken).ConfigureAwait(false);
            await stream.WriteAsync(payload, cancellationToken).ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        public static async Task<T> ReadAsync<T>(Stream stream, CancellationToken cancellationToken)
        {
            var header = new byte[sizeof(int)];
            await ReadExactlyAsync(stream, header, cancellationToken).ConfigureAwait(false);

            var payloadLength = BinaryPrimitives.ReadInt32LittleEndian(header);
            if (payloadLength <= 0 || payloadLength > MaximumMessageLength)
            {
                throw new InvalidDataException(Format(WorkbenchResources.SingleInstanceProtocolInvalidMessageLength, payloadLength));
            }

            var payload = new byte[payloadLength];
            await ReadExactlyAsync(stream, payload, cancellationToken).ConfigureAwait(false);

            return JsonSerializer.Deserialize<T>(payload) ?? throw new InvalidDataException(Format(WorkbenchResources.SingleInstanceProtocolDeserializationFailed, typeof(T).Name));
        }

        private static async Task ReadExactlyAsync(Stream stream, Memory<byte> buffer, CancellationToken cancellationToken)
        {
            var totalBytesRead = 0;

            while (totalBytesRead < buffer.Length)
            {
                var bytesRead = await stream.ReadAsync(buffer[totalBytesRead..], cancellationToken).ConfigureAwait(false);
                if (bytesRead == 0)
                {
                    throw new EndOfStreamException(WorkbenchResources.SingleInstanceProtocolConnectionEndedPrematurely);
                }

                totalBytesRead += bytesRead;
            }
        }

        private static string Format(string format, params object?[] arguments) => string.Format(CultureInfo.CurrentCulture, format, arguments);
    }

    internal sealed record SingleInstanceHandshake(int PrimaryProcessId);

    internal sealed record SingleInstanceAcknowledgement(bool Accepted);
}