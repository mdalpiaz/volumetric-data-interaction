using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Client
{
    /// <summary>
    /// Menu class
    /// Toggle between menu and detail view
    /// </summary>
    public class Menu : MonoBehaviour
    {
        [FormerlySerializedAs("client")]
        [SerializeField]
        private Networking.TabletClient tabletClient;
        [SerializeField]
        private GameObject mainMenu;
        [SerializeField]
        private GameObject interactionMenu;
        [SerializeField]
        private GameObject networkConfigMenu;
        [SerializeField]
        private Text headerText;

        private void OnEnable()
        {
            tabletClient.MenuModeChanged += HandleMenuModeChanged;
        }

        private void OnDisable()
        {
            tabletClient.MenuModeChanged -= HandleMenuModeChanged;
        }

        private void HandleMenuModeChanged(MenuMode mode)
        {
            switch (mode)
            {
                case MenuMode.Selected:
                    HandleObjectSelected();
                    break;
                case MenuMode.None:
                    Cancel();
                    break;
                default:
                    Debug.Log($"{nameof(HandleMenuModeChanged)} received unknown menu mode: {mode}");
                    break;
            }
        }

        private void HandleObjectSelected()
        {
            // set object as gameobject in a specific script?
            tabletClient.SendMenuChangedMessage(MenuMode.Selected);
        }
        
        public void SwitchToMainMenu()
        {
            mainMenu.SetActive(true);
            interactionMenu.SetActive(false);
            networkConfigMenu.SetActive(false);
        }
        
        public void StartSelection()
        {
            Debug.Log("Selection");
            tabletClient.SendMenuChangedMessage(MenuMode.Selection);
            SwitchToInteractionMenu("Selection Mode");
        }

        public void StartMapping() => tabletClient.SendMenuChangedMessage(MenuMode.Mapping);

        public void StopMapping()
        {
            tabletClient.SendTextMessage("Stopped mapping");
            HandleObjectSelected();
        }

        public void StartAnalysis()
        {
            tabletClient.SendMenuChangedMessage(MenuMode.Analysis);
            SwitchToInteractionMenu("Analysis Mode");
        }

        public void Cancel()
        {
            tabletClient.SendMenuChangedMessage(MenuMode.None);
            SwitchToMainMenu();
        }

        private void SwitchToInteractionMenu(string header)
        {
            mainMenu.SetActive(false);
            interactionMenu.SetActive(true);
            networkConfigMenu.SetActive(false);
            headerText.text = header;
        }
    }
}
