using System;
using System.Buffers.Binary;

namespace XtsCsharpClient.Services
{
    /// <summary>
    /// Demonstrates High-Performance "Zero-Copy" Binary Parsing.
    /// This uses Span<byte> to read from the memory buffer without creating extra objects.
    /// </summary>
    public class XtsBinaryParser
    {
        public static void ParseMarketTick(byte[] data)
        {
            // Zero-Copy starts here: we use a ReadOnlySpan to point to the existing data
            ReadOnlySpan<byte> span = data.AsSpan();

            // Example of reading fields directly from specific byte offsets
            // Note: These offsets are illustrative based on common binary protocol patterns
            
            // 1. Read Message Code (2 bytes at start)
            ushort msgCode = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(0, 2));
            
            // 2. Read Exchange Segment (1 byte at offset 2)
            byte segment = span[2];
            
            // 3. Read Instrument ID (4 bytes at offset 3)
            int instrumentId = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(3, 4));
            
            // 4. Read Last Traded Price (4 bytes at offset 7, multiplied by 100 usually)
            int rawPrice = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(7, 4));
            float ltp = rawPrice / 100.0f;

            Console.WriteLine($"[HIGH-SPEED BINARY] Decoded (Code: {msgCode}, Seg: {segment}, ID: {instrumentId}, Price: {ltp})");
        }
    }
}
