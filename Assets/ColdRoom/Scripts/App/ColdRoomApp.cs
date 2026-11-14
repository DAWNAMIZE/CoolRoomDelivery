using ColdRoom.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace ColdRoom.App
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class ColdRoomApp : MonoBehaviour
    {
        private UIDocument _document;
        private AppShell _shell;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            Viewport.Initialize();
        }

        private void OnEnable()
        {
            Viewport.WidthChanged += OnViewportChanged;
        }

        private void OnDisable()
        {
            Viewport.WidthChanged -= OnViewportChanged;
        }

        private void Start()
        {
            _document = GetComponent<UIDocument>();
            if (_document == null)
            {
                Debug.LogError("[ColdRoom] ColdRoomApp requires a UIDocument component.");
                return;
            }

            if (_document.panelSettings == null)
            {
                _document.panelSettings = Resources.Load<PanelSettings>("ColdRoomPanelSettings");
            }

            if (_document.panelSettings == null)
            {
                Debug.LogError("[ColdRoom] UIDocument is missing a PanelSettings asset.");
                return;
            }

            ApplyPanelScale();

            var root = _document.rootVisualElement;
            if (root == null)
            {
                Debug.LogError("[ColdRoom] UIDocument produced no root visual element.");
                return;
            }

            var styleSheet = Resources.Load<StyleSheet>("ColdRoom");
            if (styleSheet != null)
            {
                root.styleSheets.Add(styleSheet);
            }
            else
            {
                Debug.LogError("[ColdRoom] Missing Assets/ColdRoom/Resources/ColdRoom.uss.");
            }

            _shell = new AppShell(this, root);
            _shell.Build();
            _shell.HandleViewport(Viewport.Width);
            ApplyPanelScale();
        }

        private void Update()
        {
            Viewport.Poll(Time.unscaledTime);
        }

        private void OnViewportChanged(int width)
        {
            ApplyPanelScale();
            _shell?.HandleViewport(width);
        }

        private void ApplyPanelScale()
        {
            if (_document == null || _document.panelSettings == null)
            {
                return;
            }

            _document.panelSettings.scale = Viewport.PanelScale;
        }
    }
}
