#nullable enable

using System;
using System.Threading.Tasks;
using Networking.openIA.Commands;
using Networking.openIA.States;
using UnityEngine;

namespace Networking.openIA
{
    public class InterpreterV1 : ICommandInterpreter, ICommandSender
    {
        private readonly WebSocketClient _ws;
        private readonly ProtocolNegotiator _negotiator;
        private IInterpreterState _state;

        public InterpreterV1(WebSocketClient ws)
        {
            _ws = ws;
            _negotiator = new ProtocolNegotiator(_ws, this);
            _state = _negotiator;
        }

        public async Task Start() => await _negotiator.Negotiate();
        
        public async Task Interpret(byte[] data)
        {
            switch (data[0])
            {
                case Categories.ACK.Value:
                    _state = await _state.ACK();
                    break;
                case Categories.NAK.Value:
                    _state = await _state.NAK();
                    break;
                case Categories.ProtocolAdvertisement.Value:
                    _state = await _state.ProtocolAdvertisement(data);
                    break;
                case Categories.Client.Value:
                    _state = await _state.Client(data);
                    break;
                case Categories.Datasets.Value:
                    _state = await _state.Datasets(data);
                    break;
                case Categories.Objects.Value:
                    _state = await _state.Objects(data);
                    break;
                case Categories.Snapshots.Value:
                    _state = await _state.Snapshots(data);
                    break;
                default:
                    Debug.LogError($"Unknown Category received: {BitConverter.ToString(data, 0, 1)}");
                    break;
            }
        }

        public async Task Send(ICommand cmd) => await _ws.SendAsync(cmd.ToByteArray());
    }
}