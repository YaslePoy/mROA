

// public class StreamBasedFrontendInteractionModule : IInteractionModule.IFrontendInteractionModule
// {
//     public Stream? ServerStream { get; set; }
//     public int ClientId { get; set; }
//
//     public NetworkMessage[] UnhandledMessages()
//     {
//         return Array.Empty<NetworkMessage>();
//     }
//
//     public NetworkMessage LastMessage()
//     {
//         return null;
//     }
//     
//     
//     
//     public async Task<byte[]> ReceiveMessage()
//     {
//         if (ServerStream is null)
//             throw new IOException("Server is not connected.");
//
//         const int bufferSize = ushort.MaxValue;
//
//         var buffer = new byte[bufferSize];
//         if (!ServerStream.CanRead) throw new IOException("Server is not connected.");
//
//         await ServerStream.ReadExactlyAsync(buffer, 0, 2);
//         var len = BitConverter.ToUInt16(buffer, 0);
//         await ServerStream.ReadExactlyAsync(buffer, 0, len);
//
//         return buffer[..len];
//     }
//
//     public void PostMessage(byte[] message)
//     {
//         if (ServerStream is null)
//             throw new IOException("Server is not connected.");
//
//         ServerStream.Write(BitConverter.GetBytes((ushort)message.Length), 0, sizeof(ushort));
//         ServerStream.Write(message, 0, message.Length);
//     }
//
//     public void Inject<T>(T dependency)
//     {
//     }
// }