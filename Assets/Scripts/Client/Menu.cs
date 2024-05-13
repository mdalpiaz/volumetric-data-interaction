#nullable enable

using System.Threading.Tasks;
using Networking.Tablet;
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
        private TabletClient tabletClient = null!;

        [SerializeField]
        private TouchInput touchInput = null!;

        [SerializeField]
        private SpatialInput spatialInput = null!;
        
        [SerializeField]
        private GameObject mainMenu = null!;
        
        [SerializeField]
        private GameObject interactionMenu = null!;
        
        [SerializeField]
        private GameObject networkConfigMenu = null!;
        
        [SerializeField]
        private Text headerText = null!;

        private void Awake()
        {
            SwitchToMainMenu();
        }

        private void OnEnable()
        {
            tabletClient.MenuModeChanged += HandleMenuModeChanged;
            touchInput.Swiped += OnSwipe;
            touchInput.Scaled += OnScale;
            touchInput.Tapped += OnTap;
            spatialInput.Shook += OnShake;
        }

        private void OnDisable()
        {
            tabletClient.MenuModeChanged -= HandleMenuModeChanged;
            touchInput.Swiped -= OnSwipe;
            touchInput.Scaled -= OnScale;
            touchInput.Tapped -= OnTap;
            spatialInput.Shook -= OnShake;
        }

        public async void OnSelectionClick() => await StartSelection();
        
        public async void OnAnalysisClick() => await StartAnalysis();
        
        private void HandleMenuModeChanged(MenuMode mode)
        {
            switch (mode)
            {
                case MenuMode.None:
                    SwitchToMainMenu();
                    break;
                case MenuMode.Selected:
                // don't send menu mode change back to server (AGAIN)
                //await tabletClient.SendMenuChangedMessage(MenuMode.Selected);
                case MenuMode.Analysis:
                case MenuMode.Selection:
                    break;
                default:
                    Debug.Log($"{nameof(HandleMenuModeChanged)} received unknown menu mode: {mode}");
                    break;
            }
        }
        
        private void SwitchToMainMenu()
        {
            mainMenu.SetActive(true);
            interactionMenu.SetActive(false);
            networkConfigMenu.SetActive(false);
        }
        
        private async Task StartSelection()
        {
            Debug.Log("Selection");
            SwitchToInteractionMenu("Selection Mode");
            await tabletClient.SendMenuChangedMessage(MenuMode.Selection);
        }

        private async Task StartAnalysis()
        {
            SwitchToInteractionMenu("Analysis Mode");
            await tabletClient.SendMenuChangedMessage(MenuMode.Analysis);
        }
        
        private async Task Cancel()
        {
            SwitchToMainMenu();
            await tabletClient.SendMenuChangedMessage(MenuMode.None);
        }

        private void SwitchToInteractionMenu(string header)
        {
            mainMenu.SetActive(false);
            interactionMenu.SetActive(true);
            networkConfigMenu.SetActive(false);
            headerText.text = header;
        }

        private async void OnSwipe(bool inward, float endPointX, float endPointY, float angle)
        {
            var netTask = tabletClient.SendSwipeMessage(inward, endPointX, endPointY, angle);
            var cancelTask = inward ? Cancel() : Task.CompletedTask;
            await Task.WhenAll(netTask, cancelTask);
        }

        private async void OnScale(float scale) => await tabletClient.SendScaleMessage(scale);

        private async void OnTap(TapType type, float x, float y) => await tabletClient.SendTapMessage(type, x, y);

        private async void OnShake(int shakeCount) => await tabletClient.SendShakeMessage(shakeCount);
    }
}
