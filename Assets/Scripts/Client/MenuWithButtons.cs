using Networking.Tablet;
using TMPro;
using UnityEngine;

namespace Client
{
    public class MenuWithButtons : MonoBehaviour
    {
        [SerializeField]
        private TabletClient tabletClient = null!;

        [SerializeField]
        private GameObject networkingPanel = null!;

        [SerializeField]
        private GameObject modePanel = null!;

        [SerializeField]
        private GameObject slicingPanel = null!;

        [SerializeField]
        private GameObject selectPanel = null!;

        [SerializeField]
        private TMP_InputField ipInput = null!;

        public async void OnConnectClicked()
        {
            tabletClient.IP = ipInput.text;
            await tabletClient.Connect();
            networkingPanel.SetActive(false);
        }
        
        public void OnSlicingMode()
        {
            selectPanel.SetActive(false);
            slicingPanel.SetActive(true);
        }

        public void OnSelectMode()
        {
            slicingPanel.SetActive(false);
            selectPanel.SetActive(true);
        }

        public async void OnSlice()
        {
            await tabletClient.SendTapMessage(TapType.Double, 250, 250);
        }

        public void OnToggleAttach()
        {
            
        }

        public void OnResetSnapshots()
        {
            
        }

        public void OnRemoveSnapshot()
        {
            
        }

        public void OnResetModel()
        {
            
        }

        public void OnSelect()
        {
            
        }
    }
}