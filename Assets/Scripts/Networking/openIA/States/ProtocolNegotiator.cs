#nullable enable

using System;
using System.Threading.Tasks;
using System.Buffers.Binary;

namespace Networking.openIA.States
{
    public class ProtocolNegotiator : IInterpreterState
    {
        private ulong _protocolVersion;

        private readonly WebSocketClient _ws;

        private readonly ICommandSender _sender;

        public ProtocolNegotiator(WebSocketClient ws, ICommandSender sender)
        {
            _ws = ws;
            _sender = sender;
        }
        
        public async Task Negotiate()
        {
            var request = new byte[1 + sizeof(ulong)];
            request[0] = Categories.ProtocolAdvertisement.Value;
            var versionBytes = BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness(1L));
            _protocolVersion = 1;
            Buffer.BlockCopy(versionBytes, 0, request, 1, sizeof(ulong));
            await _ws.SendAsync(request);
        }

        public Task<IInterpreterState> ACK()
        {
            return Task.FromResult<IInterpreterState>(new DefaultStateV1(_sender));
        }
        public Task<IInterpreterState> NAK()
        {
            return Task.FromResult<IInterpreterState>(this);
        }
    }
}