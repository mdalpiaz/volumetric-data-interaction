#nullable enable

using Extensions;
using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;

namespace Networking.Tablet
{
    public class TabletClient : MonoBehaviour
    {
        [SerializeField]
        private string ip = "127.0.0.1";

        [SerializeField]
        private int port = Ports.TabletPort;

        public string IP
        {
            get => ip;
            set => ip = value;
        }

        public int Port
        {
            get => port;
            set => port = value;
        }
        
        private TcpClient tcpClient = null!;
        private NetworkStream stream = null!;
        
        public event Action? ModelSelected;

        public event Action? SnapshotSelected;

        public event Action? SnapshotRemoved;

        private void Awake()
        {
            tcpClient = new TcpClient();
        }

        private void OnDestroy()
        {
            tcpClient.Close();
        }

        public void Connect()
        {
            tcpClient.Connect(IP, Port);
            stream = tcpClient.GetStream();
            Debug.Log("Connected to server");
        }

        public async Task Run()
        {
            var buffer = new byte[1];
            while (true)
            {
                try
                {
                    await stream.ReadAllAsync(buffer, 0, 1);
                }
                catch
                {
                    break;
                }
                switch (buffer[0])
                {
                    case Categories.SelectedModel:
                        ModelSelected?.Invoke();
                        break;
                    case Categories.SelectedSnapshot:
                        SnapshotSelected?.Invoke();
                        break;
                    case Categories.SnapshotRemoved:
                        SnapshotRemoved?.Invoke();
                        break;
                    default:
                        Debug.LogWarning("Unsupported command was received!");
                        break;
                }
            }
        }

        public void Send(ICommand cmd)
        {
            stream.Write(cmd.ToByteArray());
        }
        
        public void Send(byte command)
        {
            stream.WriteByte(command);
        }
    }
}
