#nullable enable

using Networking.Tablet;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Client
{
    public class MenuWithButtons : MonoBehaviour
    {
        [SerializeField]
        private TabletClient tabletClient = null!;

        [SerializeField]
        private TouchInput touchInput = null!;

        [SerializeField]
        private GameObject networkingPanel = null!;

        [SerializeField]
        private GameObject modePanel = null!;

        [SerializeField]
        private GameObject slicingPanel = null!;

        [SerializeField]
        private GameObject modelSelectedPanel = null!;

        [SerializeField]
        private GameObject snapshotSelectedPanel = null!;

        [SerializeField]
        private TMP_InputField ipInput = null!;

        private Task? runningTask;

        private void OnEnable()
        {
            tabletClient.ModelSelected += OnModelSelected;
            tabletClient.SnapshotSelected += OnSnapshotSelected;
            tabletClient.SnapshotRemoved += OnSnapshotRemoved;
            touchInput.Swiped += OnSwipe;
            touchInput.Scaled += OnScale;
            // touchInput.Tapped += OnTap;
        }

        private void OnDisable()
        {
            tabletClient.ModelSelected -= OnModelSelected;
            tabletClient.SnapshotSelected -= OnSnapshotSelected;
            tabletClient.SnapshotRemoved -= OnSnapshotRemoved;
            touchInput.Swiped -= OnSwipe;
            touchInput.Scaled -= OnScale;
            // touchInput.Tapped -= OnTap;
        }

        private async void OnDestroy()
        {
            if (runningTask != null)
            {
                await runningTask;
            }
        }

        public void ConnectClicked()
        {
            Debug.Log("Clicked");
            tabletClient.IP = ipInput.text.Trim();
            Debug.Log($"IP: {ipInput.text.Trim()}");
            tabletClient.Connect();
            Debug.Log("Connected");
            runningTask = tabletClient.Run();
            SelectionMode();
        }

        public void Slice() => tabletClient.Send(Categories.Slice);

        public void ToggleAttached() => tabletClient.Send(Categories.ToggleAttached);

        public void RemoveSnapshot() => tabletClient.Send(Categories.RemoveSnapshot);

        public void Select() => tabletClient.Send(Categories.Select);

        public void Unselect() => tabletClient.Send(Categories.Deselect);

        public void SlicingMode()
        {
            tabletClient.Send(Categories.SlicingMode);
            DeactivateAll();
            slicingPanel.SetActive(true);
        }

        public void SelectionMode()
        {
            tabletClient.Send(Categories.SelectionMode);
            DeactivateAll();
            modePanel.SetActive(true);
        }
        
        public void OnPointerDown()
        {
            tabletClient.Send(Categories.HoldBegin);
        }

        public void OnPointerUp()
        {
            tabletClient.Send(Categories.HoldEnd);
        }

        private void OnModelSelected()
        {
            DeactivateAll();
            modelSelectedPanel.SetActive(true);
        }

        private void OnSnapshotSelected()
        {
            DeactivateAll();
            snapshotSelectedPanel.SetActive(true);
        }

        private void OnSnapshotRemoved()
        {
            if (!snapshotSelectedPanel.activeInHierarchy)
            {
                return;
            }

            SelectionMode();
        }

        private void OnSwipe(bool inward, float endPointX, float endPointY, float angle)
        {
            if (!snapshotSelectedPanel.activeInHierarchy)
            {
                return;
            }

            var direction = DirectionMethods.GetDirectionDegree(angle);
            if (direction == Direction.Up)
            {
                tabletClient.Send(Categories.SendToScreen);
            }
        }

        private void OnScale(float scale)
        {
            if (modelSelectedPanel.activeInHierarchy)
            {
                tabletClient.Send(new ScaleCommand(scale));
            }
        }

        private void OnTap(TapType tapType, float x, float y)
        {
            if (tapType == TapType.HoldBegin)
            {
                tabletClient.Send(Categories.HoldBegin);
            }
            else if (tapType == TapType.HoldEnd)
            {
                tabletClient.Send(Categories.HoldEnd);
            }
        }

        private void DeactivateAll()
        {
            networkingPanel.SetActive(false);
            modePanel.SetActive(false);
            slicingPanel.SetActive(false);
            modelSelectedPanel.SetActive(false);
            snapshotSelectedPanel.SetActive(false);
        }
    }
}