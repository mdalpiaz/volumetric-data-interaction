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
            touchInput.Rotated += OnRotate;
            touchInput.Tapped += OnTap;
            spatialInput.Shook += OnShake;
            spatialInput.Tilted += OnTilt;
        }

        private void OnDisable()
        {
            tabletClient.MenuModeChanged -= HandleMenuModeChanged;
            touchInput.Swiped -= OnSwipe;
            touchInput.Scaled -= OnScale;
            touchInput.Rotated -= OnRotate;
            touchInput.Tapped -= OnTap;
            spatialInput.Shook -= OnShake;
            spatialInput.Tilted -= OnTilt;
        }

        public async void OnSelectionClick() => await StartSelection();
        
        public async void OnAnalysisClick() => await StartAnalysis();
        
        private async void HandleMenuModeChanged(MenuMode mode)
        {
            switch (mode)
            {
                case MenuMode.Selected:
                    await tabletClient.SendMenuChangedMessage(MenuMode.Selected);
                    break;
                case MenuMode.None:
                    await Cancel();
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
            await tabletClient.SendMenuChangedMessage(MenuMode.Selection);
            SwitchToInteractionMenu("Selection Mode");
        }

        private async Task StartAnalysis()
        {
            await tabletClient.SendMenuChangedMessage(MenuMode.Analysis);
            SwitchToInteractionMenu("Analysis Mode");
        }
        
        private async Task Cancel()
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

        private async void OnSwipe(bool inward, float endPointX, float endPointY, float angle)
        {
            var netTask = tabletClient.SendSwipeMessage(inward, endPointX, endPointY, angle);
            var cancelTask = inward ? Cancel() : Task.CompletedTask;
            await Task.WhenAll(netTask, cancelTask);
        }

        private async void OnScale(float scale) => await tabletClient.SendScaleMessage(scale);

        private async void OnRotate(float angle) => await tabletClient.SendRotateMessage(angle);

        private async void OnTap(TapType type, float x, float y) => await tabletClient.SendTapMessage(type, x, y);

        private async void OnShake(int shakeCount) => await tabletClient.SendShakeMessage(shakeCount);

        private async void OnTilt(bool isLeft) => await tabletClient.SendTiltMessage(isLeft);
    }
}
