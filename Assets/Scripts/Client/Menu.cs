using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Client
{
    /// <summary>
    /// Menu class
    /// Toggle between menu and detail view
    /// </summary>
    public class Menu : MonoBehaviour
    {
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

        public async void OnSelectionClick()
        {
            await StartSelection();
        }
        
        public async void OnAnalysisClick()
        {
            await StartAnalysis();
        }
        
        private async void HandleMenuModeChanged(MenuMode mode)
        {
            switch (mode)
            {
                case MenuMode.Selected:
                    await HandleObjectSelected();
                    break;
                case MenuMode.None:
                    await Cancel();
                    break;
                default:
                    Debug.Log($"{nameof(HandleMenuModeChanged)} received unknown menu mode: {mode}");
                    break;
            }
        }

        private async Task HandleObjectSelected()
        {
            // set object as gameobject in a specific script?
            await tabletClient.SendMenuChangedMessage(MenuMode.Selected);
        }
        
        public void SwitchToMainMenu()
        {
            mainMenu.SetActive(true);
            interactionMenu.SetActive(false);
            networkConfigMenu.SetActive(false);
        }
        
        private async Task StartSelection()
        {
            Debug.Log("Selection");
            await tabletClient.SendMenuChangedMessage(MenuMode.Selection);
            SwitchToInteractionMenu("Selection Mode");
        }

        public async Task StartMapping() => await tabletClient.SendMenuChangedMessage(MenuMode.Mapping);

        public async Task StopMapping() => await HandleObjectSelected();

        private async Task StartAnalysis()
        {
            await tabletClient.SendMenuChangedMessage(MenuMode.Analysis);
            SwitchToInteractionMenu("Analysis Mode");
        }

        public async Task Cancel()
        {
            await tabletClient.SendMenuChangedMessage(MenuMode.None);
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
