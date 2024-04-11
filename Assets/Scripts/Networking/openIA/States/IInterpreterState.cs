#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using UnityEngine;

namespace Networking.openIA.States
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public interface IInterpreterState
    {
        public virtual Task<IInterpreterState> ACK()
        {
            Debug.LogWarning($"Message {nameof(ACK)} not implemented");
            return Task.FromResult(this);
        }
        public virtual Task<IInterpreterState> NAK()
        {
            Debug.LogWarning($"Message {nameof(NAK)} not implemented");
            return Task.FromResult(this);
        }
        public virtual Task<IInterpreterState> ProtocolAdvertisement(byte[] data)
        {
            Debug.LogWarning($"Message {nameof(ProtocolAdvertisement)} not implemented");
            return Task.FromResult(this);
        }
        public virtual Task<IInterpreterState> Client(byte[] data)
        {
            Debug.LogWarning($"Message {nameof(Client)} not implemented");
            return Task.FromResult(this);
        }
        public virtual Task<IInterpreterState> Datasets(byte[] data)
        {
            Debug.LogWarning($"Message {nameof(Datasets)} not implemented");
            return Task.FromResult(this);
        }
        public virtual Task<IInterpreterState> Objects(byte[] data)
        {
            Debug.LogWarning($"Message {nameof(Objects)} not implemented");
            return Task.FromResult(this);
        }
        public virtual Task<IInterpreterState> Snapshots(byte[] data)
        {
            Debug.LogWarning($"Message {nameof(Snapshots)} not implemented");
            return Task.FromResult(this);
        }
    }
}