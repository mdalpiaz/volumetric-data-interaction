#nullable enable

using System;
using System.Linq;
using System.Threading.Tasks;
using Model;
using Snapshots;
using UnityEngine;

namespace Networking.openIA.States
{
    public class DefaultStateV1 : IInterpreterState
    {
        private readonly ICommandSender _sender;

        public DefaultStateV1(ICommandSender sender)
        {
            _sender = sender;
        }

        public Task<IInterpreterState> Client(byte[] data)
        {
            var client = ClientLoginResponse.FromByteArray(data);
            Debug.Log($"Received client id: {client.ID}");
            OpenIAWebSocketClient.Instance.ClientID = client.ID;
            return Task.FromResult<IInterpreterState>(this);
        }

        public async Task<IInterpreterState> Datasets(byte[] data)
        {
            switch (data[1])
            {
                case Categories.Datasets.Reset:
                {
                    ModelManager.Instance.CurrentModel.ResetState();
                    SnapshotManager.Instance.DeleteAllSnapshots();
                    return this;
                }
                case Categories.Datasets.LoadDataset:
                {
                    var loadCommand = LoadDataset.FromByteArray(data);
                    if (ModelManager.Instance.ModelExists(loadCommand.Name))
                    {
                        await _sender.Send(new ACK());
                        return new WaitingForServerACK(this, () => ModelManager.Instance.ChangeModel(loadCommand.Name));
                    }

                    await _sender.Send(new NAK());
                    return this;
                }
                default:
                {
                    Debug.LogError($"Unhandled Subcategory in Dataset Operation: {BitConverter.ToString(data, 1, 1)}");
                    return this;
                }
            }
        }

        public Task<IInterpreterState> Objects(byte[] data)
        {
            switch (data[1])
            {
                case Categories.Objects.SetMatrix:
                {
                    var matrixCommand = SetObjectMatrix.FromByteArray(data);
                    MatrixToObject(matrixCommand.ID, matrixCommand.Matrix);
                    break;
                }
                case Categories.Objects.Translate:
                {
                    var translateCommand = SetObjectTranslation.FromByteArray(data);
                    TranslateObject(translateCommand.ID, translateCommand.Translation);
                    break;
                }
                case Categories.Objects.Scale:
                {
                    var scaleCommand = SetObjectScale.FromByteArray(data);
                    ScaleObject(scaleCommand.ID, scaleCommand.Scale);
                    break;
                }
                case Categories.Objects.RotateQuaternion:
                {
                    var rotateCommand = SetObjectRotationQuaternion.FromByteArray(data);
                    RotateObject(rotateCommand.ID, rotateCommand.Rotation);
                    break;
                }
                case Categories.Objects.RotateEuler:
                {
                    var rotateCommand = SetObjectRotationEuler.FromByteArray(data);
                    RotateObject(rotateCommand.ID, rotateCommand.Axis, rotateCommand.Value);
                    break;
                }
                case Categories.Objects.RotateNormalAndUp:
                {
                    var rotateCommand = SetObjectRotationNormal.FromByteArray(data);
                    RotateObject(rotateCommand.ID, Quaternion.LookRotation(rotateCommand.Normal, rotateCommand.Up));
                    break;
                }
            }

            return Task.FromResult<IInterpreterState>(this);
        }

        public Task<IInterpreterState> Snapshots(byte[] data)
        {
            switch (data[1])
            {
                case Categories.Snapshots.CreateQuaternion:
                {
                    var createCommand = CreateSnapshotQuaternionServer.FromByteArray(data);
                    var currentModel = ModelManager.Instance.CurrentModel;
                    var unityCoords = CoordinateConverter.OpenIAToUnity(currentModel, createCommand.Position);
                    var worldCoords = currentModel.transform.TransformPoint(unityCoords);
                    SnapshotManager.Instance.CreateSnapshot(createCommand.ID, worldCoords, createCommand.Rotation);
                    break;
                }
                case Categories.Snapshots.CreateNormal:
                {
                    var createCommand = CreateSnapshotNormalServer.FromByteArray(data);
                    var currentModel = ModelManager.Instance.CurrentModel;
                    var unityCoords = CoordinateConverter.OpenIAToUnity(currentModel, createCommand.Position);
                    var worldCoords = currentModel.transform.TransformPoint(unityCoords);
                    var worldNormal = currentModel.transform.TransformVector(createCommand.Normal);
                    var worldQuaternion = Quaternion.LookRotation(worldNormal);
                    SnapshotManager.Instance.CreateSnapshot(createCommand.ID, worldCoords, worldQuaternion);
                    break;
                }
                case Categories.Snapshots.Remove:
                {
                    var removeCommand = RemoveSnapshot.FromByteArray(data);
                    SnapshotManager.Instance.DeleteSnapshot(removeCommand.ID);
                    break;
                }
                case Categories.Snapshots.Clear:
                {
                    SnapshotManager.Instance.DeleteAllSnapshots();
                    break;
                }
            }

            return Task.FromResult<IInterpreterState>(this);
        }

        private static void MatrixToObject(ulong id, Matrix4x4 matrix)
        {
            if (id == 1)
            {
                Debug.LogWarning("Tried changing the slicing plane. Stopped.");
                return;
            }

            var model = ModelManager.Instance.CurrentModel;
            var position = matrix.GetPosition();
            var convertedPosition = CoordinateConverter.OpenIAToUnity(model, position);
            
            if (id == 0)
            {
                model.transform.SetPositionAndRotation(convertedPosition, matrix.rotation);
                model.transform.localScale = matrix.lossyScale;
                return;
            }

            var viewer = OpenIAWebSocketClient.Instance.Viewers.FirstOrDefault(v => v.ID == id);
            if (viewer != null)
            {
                viewer.transform.SetPositionAndRotation(convertedPosition, matrix.rotation);
            }
        }

        private static void TranslateObject(ulong id, Vector3 translation)
        {
            if (id == 1)
            {
                Debug.LogWarning("Tried changing the slicing plane. Stopped.");
                return;
            }

            var model = ModelManager.Instance.CurrentModel;
            var convertedPosition = CoordinateConverter.OpenIAToUnity(model, translation);
            
            if (id == 0)
            {
                model.transform.position = convertedPosition;
                return;
            }

            var viewer = OpenIAWebSocketClient.Instance.Viewers.FirstOrDefault(v => v.ID == id);
            if (viewer != null)
            {
                viewer.transform.position = convertedPosition;
            }
        }

        private static void ScaleObject(ulong id, Vector3 scale)
        {
            if (id == 1)
            {
                Debug.LogWarning("Tried changing the slicing plane. Stopped.");
                return;
            }
            
            if (id == 0)
            {
                var model = ModelManager.Instance.CurrentModel;
                model.transform.localScale = scale;
                return;
            }
            
            Debug.Log("Tried to change one of the viewers. Stopped.");
        }
        
        private static void RotateObject(ulong id, Axis axis, float value)
        {
            if (id == 1)
            {
                Debug.LogWarning("Tried changing the slicing plane. Stopped.");
                return;
            }

            var vec = (byte)axis switch
            {
                0 => new Vector3(1, 0, 0),
                1 => new Vector3(0, 1, 0),
                2 => new Vector3(0, 0, 1),
                _ => throw new ArgumentException($"Invalid axis: {axis}")
            };

            if (id == 0)
            {
                ModelManager.Instance.CurrentModel.transform.Rotate(vec, value);
                return;
            }
            
            var viewer = OpenIAWebSocketClient.Instance.Viewers.FirstOrDefault(v => v.ID == id);
            if (viewer != null)
            {
                viewer.transform.Rotate(vec, value);
            }
        }

        private static void RotateObject(ulong id, Quaternion quaternion)
        {
            if (id == 1)
            {
                Debug.LogWarning("Tried changing the slicing plane. Stopped.");
                return;
            }

            if (id == 0)
            {
                ModelManager.Instance.CurrentModel.transform.rotation = quaternion;
                return;
            }

            var viewer = OpenIAWebSocketClient.Instance.Viewers.FirstOrDefault(v => v.ID == id);
            if (viewer != null)
            {
                viewer.transform.rotation = quaternion;
            }
        }
    }
}