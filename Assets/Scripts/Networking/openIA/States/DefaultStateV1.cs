#nullable enable

using System;
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
            // INFO id is entirely unused on the client
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
            }

            return Task.FromResult<IInterpreterState>(this);
        }

        public Task<IInterpreterState> Snapshots(byte[] data)
        {
            switch (data[1])
            {
                case Categories.Snapshots.Create:
                {
                    var createCommand = CreateSnapshotServer.FromByteArray(data);
                    SnapshotManager.Instance.CreateSnapshot(createCommand.ID, createCommand.Position, createCommand.Rotation);
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
            if (id != 0)
            {
                // my slicing plane is the tablet...
                // how would this work?
                Debug.LogWarning("The SlicingPlane is the Tablet. It cannot be changed.");
                return;
            }

            var transform = ModelManager.Instance.CurrentModel.transform;
            transform.SetPositionAndRotation(matrix.GetPosition(), matrix.rotation);
            transform.localScale = matrix.lossyScale;
        }

        private static void TranslateObject(ulong id, Vector3 translation)
        {
            if (id != 0)
            {
                // my slicing plane is the tablet...
                // how would this work?
                Debug.LogWarning("The SlicingPlane is the Tablet. It cannot be changed.");
                return;
            }
            
            translation = CoordinateConverter.OpenIAToUnity(translation);
            ModelManager.Instance.CurrentModel.transform.position += translation;
        }

        private static void ScaleObject(ulong id, Vector3 scale)
        {
            if (id != 0)
            {
                // my slicing plane is the tablet...
                // how would this work?
                Debug.LogWarning("The SlicingPlane is the Tablet. It cannot be changed.");
                return;
            }

            ModelManager.Instance.CurrentModel.transform.localScale += scale;
        }
        
        private static void RotateObject(ulong id, Axis axis, float value)
        {
            if (id != 0)
            {
                // my slicing plane is the tablet...
                // how would this work?
                Debug.LogWarning("The SlicingPlane is the Tablet. It cannot be changed.");
                return;
            }

            var vec = (byte)axis switch
            {
                0 => new Vector3(1, 0, 0),
                1 => new Vector3(0, 1, 0),
                2 => new Vector3(0, 0, 1),
                _ => throw new ArgumentException($"Invalid axis: {axis}")
            };
            
            ModelManager.Instance.CurrentModel.transform.Rotate(vec, value);
        }

        private static void RotateObject(ulong id, Quaternion quaternion)
        {
            if (id != 0)
            {
                // my slicing plane is the tablet...
                // how would this work?
                Debug.LogWarning("The SlicingPlane is the Tablet. It cannot be changed.");
                return;
            }

            ModelManager.Instance.CurrentModel.transform.rotation *= quaternion;
        }
    }
}