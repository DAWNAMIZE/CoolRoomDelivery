using System;
using System.Threading.Tasks;
using ColdRoom.Api;
using ColdRoom.Core;
using ColdRoom.Core.Models;
using UnityEngine;
using UnityEngine.UIElements;

namespace ColdRoom.App
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class ColdRoomApp : MonoBehaviour
    {
        private UIDocument _document;
        private VisualElement _root;
        private AppShell _shell;
        private IColdRoomApi _api;

        public IColdRoomApi Api => _api;

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

            _root = _document.rootVisualElement;
            if (_root == null)
            {
                Debug.LogError("[ColdRoom] UIDocument produced no root visual element.");
                return;
            }

            var styleSheet = Resources.Load<StyleSheet>("ColdRoom");
            if (styleSheet != null)
            {
                _root.styleSheets.Add(styleSheet);
            }
            else
            {
                Debug.LogError("[ColdRoom] Missing Assets/ColdRoom/Resources/ColdRoom.uss.");
            }

            _ = BootAsync();
        }

        private async Task BootAsync()
        {
            AppConfig config = AppConfig.Load();
            _api = config.useMockApi
                ? new MockColdRoomApi()
                : (IColdRoomApi)new RestColdRoomApi(config.apiBaseUrl);

            ShowBoot("Restoring your session\u2026");

            User user = null;
            try
            {
                user = await _api.GetMe();
            }
            catch (ApiException)
            {
                SessionStore.Clear();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[ColdRoom] Session restore failed: {exception.Message}");
                SessionStore.Clear();
            }

            if (user != null)
            {
                ShowShell(user);
            }
            else
            {
                ShowAuth();
            }
        }

        private void ShowBoot(string message)
        {
            _root.Clear();
            _root.AddToClassList("app");
            _root.RemoveFromClassList("auth-page");
            ApplyViewportClasses(Viewport.Width);

            var page = Ui.Col("auth-page");
            var card = Ui.Col("card", "boot-card");
            card.Add(Ui.Monogram("CS", "brand-mark"));
            card.Add(Ui.Text("CamaanStores", "boot-brand"));
            card.Add(Ui.Text(message, "boot-copy"));
            page.Add(card);
            _root.Add(page);
        }

        private void ShowAuth()
        {
            _shell = null;
            _root.Clear();
            _root.RemoveFromClassList("auth-page");
            _root.AddToClassList("app");
            ApplyViewportClasses(Viewport.Width);

            var auth = new AuthScreen(_api, result => ShowShell(result.User));
            auth.Build(_root);
        }

        private void ShowShell(User user)
        {
            _root.Clear();
            _root.RemoveFromClassList("auth-page");
            _root.AddToClassList("app");
            ApplyViewportClasses(Viewport.Width);

            _shell = new AppShell(_root, _api, user, LogoutAsync);
            _shell.Build();
            _shell.HandleViewport(Viewport.Width);
        }

        public async Task LogoutAsync()
        {
            try
            {
                await _api.Logout();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[ColdRoom] Logout failed: {exception.Message}");
            }

            SessionStore.Clear();
            _shell = null;
            ShowAuth();
        }

        private void Update()
        {
            Viewport.Poll(Time.unscaledTime);
        }

        private void OnViewportChanged(int width)
        {
            ApplyPanelScale();
            ApplyViewportClasses(width);
            _shell?.HandleViewport(width);
        }

        private void ApplyViewportClasses(int width)
        {
            if (_root == null)
            {
                return;
            }

            bool compact = width < Viewport.CompactBreakpoint;
            bool narrow = !compact && width < Viewport.NarrowBreakpoint;
            _root.EnableInClassList("compact", compact);
            _root.EnableInClassList("wide", !compact);
            _root.EnableInClassList("narrow", narrow);
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
