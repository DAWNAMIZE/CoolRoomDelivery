using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ColdRoom.Api;
using ColdRoom.App.Screens;
using ColdRoom.Core;
using ColdRoom.Core.Models;
using UnityEngine;
using UnityEngine.UIElements;

namespace ColdRoom.App
{
    public sealed class AppShell
    {
        private static readonly Dictionary<UserRole, string[]> NavByRole = new()
        {
            { UserRole.Customer, new[] { "shop", "cart", "orders", "storage", "profile" } },
            { UserRole.Operator, new[] { "operator", "operator-storage", "profile" } },
            { UserRole.Rider, new[] { "rider", "profile" } }
        };

        private readonly VisualElement _appRoot;
        private readonly IColdRoomApi _api;
        private readonly Func<Task> _logout;
        private readonly Dictionary<string, ScreenView> _screens = new();
        private readonly List<(string Route, Button Button)> _railButtons = new();
        private readonly List<(string Route, Button Button)> _tabButtons = new();

        private User _user;
        private VisualElement _shell;
        private VisualElement _railNav;
        private VisualElement _tabbar;
        private ScrollView _content;
        private Label _breadcrumb;
        private Label _clock;
        private Label _railName;
        private Label _railEmail;
        private Label _railAvatarText;
        private Label _wideAvatarText;
        private Label _compactAvatarText;
        private Label _compactRoleLabel;
        private Label _layoutChip;
        private Label _toastLabel;
        private VisualElement _toastHost;
        private VisualElement _toast;
        private IVisualElementScheduledItem _toastTimer;
        private string _route;
        private ScreenView _currentScreen;

        public AppShell(VisualElement appRoot, IColdRoomApi api, User user, Func<Task> logout)
        {
            _appRoot = appRoot;
            _api = api;
            _user = user;
            _logout = logout;
        }

        public User User => _user;
        public IColdRoomApi Api => _api;
        public UserRole Role => _user.Role;
        public string RoleLabel => _user.Role.ToString();
        public string DisplayName => _user.FullName;
        public string Email => _user.Email;
        public string Initials => InitialsOf(_user.FullName);
        public string CurrentRoute => _route;

        public void Build()
        {
            _appRoot.Clear();
            _appRoot.AddToClassList("app");
            _appRoot.RemoveFromClassList("auth-page");

            RegisterScreens();

            _shell = Ui.El("shell");
            _appRoot.Add(_shell);

            _shell.Add(BuildRail());
            _shell.Add(BuildMain());

            BuildToast();
            UpdatePersonChrome();

            _shell.schedule.Execute(UpdateClock).Every(1000);
            BuildNav();
            Navigate(DefaultRoute(Role));
        }

        private void RegisterScreens()
        {
            Register(new ShopScreen());
            Register(new CartScreen());
            Register(new OrdersScreen());
            Register(new StorageScreen());
            Register(new ProfileScreen());
            Register(new OperatorScreen());
            Register(new OperatorStorageScreen());
            Register(new RiderScreen());
        }

        private void Register(ScreenView screen)
        {
            _screens[screen.Route] = screen;
        }

        private VisualElement BuildRail()
        {
            var rail = Ui.Col("rail");

            var brand = Ui.Row("brand");
            brand.Add(Ui.Monogram("CS", "brand-mark"));
            var brandCopy = Ui.Col("brand-copy");
            brandCopy.Add(Ui.Text("CamaanStores", "brand-name"));
            brandCopy.Add(Ui.Text("Lagos cold-chain", "brand-sub"));
            brand.Add(brandCopy);
            rail.Add(brand);

            var roleBlock = Ui.Col("role-block");
            roleBlock.Add(Ui.Text("SIGNED IN AS", "rail-label"));
            var roleRow = Ui.Row("rail-role-row");
            roleRow.Add(Ui.Pill(RoleLabel, "pill-accent"));
            roleRow.Add(Ui.Text("Session active", "rail-role-note"));
            roleBlock.Add(roleRow);
            rail.Add(roleBlock);

            _railNav = Ui.Col("rail-nav");
            rail.Add(_railNav);

            rail.Add(Ui.Spacer());

            var person = Ui.Row("rail-person");
            var avatar = Ui.Avatar(Initials, "avatar-sm");
            _railAvatarText = avatar.Q<Label>();
            person.Add(avatar);
            var personCopy = Ui.Col("rail-person-copy");
            _railName = Ui.Text(DisplayName, "rail-person-name");
            _railEmail = Ui.Text(Email, "rail-person-email");
            personCopy.Add(_railName);
            personCopy.Add(_railEmail);
            person.Add(personCopy);
            Ui.Clickable(person, () => Navigate("profile"));
            rail.Add(person);

            var footer = Ui.Col("rail-footer");
            footer.Add(Ui.Divider());
            var footerRow = Ui.Row("rail-footer-row");
            footerRow.Add(Ui.Dot());
            footerRow.Add(Ui.Text("Prototype build", "rail-footer-text"));
            footerRow.Add(Ui.Spacer());
            _layoutChip = Ui.Text("Wide", "layout-chip");
            footerRow.Add(_layoutChip);
            footer.Add(footerRow);
            footer.Add(Ui.Text("Step 2 of 11 \u00B7 Auth & profile", "rail-footer-sub"));
            rail.Add(footer);

            return rail;
        }

        private VisualElement BuildMain()
        {
            var main = Ui.Col("main");
            main.Add(BuildWideTopbar());
            main.Add(BuildCompactTopbar());

            _content = new ScrollView(ScrollViewMode.Vertical)
            {
                horizontalScrollerVisibility = ScrollerVisibility.Hidden,
                verticalScrollerVisibility = ScrollerVisibility.Hidden,
                touchScrollBehavior = ScrollView.TouchScrollBehavior.Clamped
            };
            _content.AddToClassList("content");
            main.Add(_content);

            _tabbar = Ui.Row("tabbar");
            main.Add(_tabbar);

            return main;
        }

        private VisualElement BuildWideTopbar()
        {
            var bar = Ui.Row("topbar", "topbar-wide");

            var status = Ui.Row("station-status");
            status.Add(Ui.Dot());
            status.Add(Ui.Text("All 4 stations online", "status-text"));
            bar.Add(status);

            bar.Add(Ui.Spacer());

            _breadcrumb = Ui.Text("SHOP", "breadcrumb-page");
            bar.Add(Ui.Text("CAMAANSTORES", "breadcrumb-root"));
            bar.Add(Ui.Text("/", "breadcrumb-sep"));
            bar.Add(_breadcrumb);

            bar.Add(Ui.El("topbar-sep"));

            var clockCopy = Ui.Col("clock-copy");
            clockCopy.Add(Ui.Text("LAGOS", "clock-label"));
            _clock = Ui.Text(LagosTime.Clock, "clock");
            clockCopy.Add(_clock);
            bar.Add(clockCopy);

            var avatar = Ui.Avatar(Initials, "avatar-sm", "topbar-avatar");
            _wideAvatarText = avatar.Q<Label>();
            Ui.Clickable(avatar, () => Navigate("profile"));
            bar.Add(avatar);

            return bar;
        }

        private VisualElement BuildCompactTopbar()
        {
            var bar = Ui.Row("topbar", "topbar-compact");

            var brand = Ui.Row("compact-brand");
            brand.Add(Ui.Monogram("CS", "brand-mark-sm"));
            brand.Add(Ui.Text("CamaanStores", "compact-brand-name"));
            bar.Add(brand);

            bar.Add(Ui.Spacer());

            _compactRoleLabel = Ui.Text(RoleLabel, "role-pill-static");
            bar.Add(_compactRoleLabel);

            var avatar = Ui.Avatar(Initials, "avatar-xs");
            _compactAvatarText = avatar.Q<Label>();
            Ui.Clickable(avatar, () => Navigate("profile"));
            bar.Add(avatar);

            return bar;
        }

        private void BuildToast()
        {
            _toastHost = Ui.El("toast-host");
            _toast = Ui.Row("toast");
            _toast.Add(Ui.Dot("toast-dot"));
            _toastLabel = Ui.Text(string.Empty, "toast-text");
            _toast.Add(_toastLabel);
            _toast.RegisterCallback<ClickEvent>(_ => HideToast());
            _toastHost.Add(_toast);
            _toastHost.style.display = DisplayStyle.None;
            _appRoot.Add(_toastHost);
        }

        private void BuildNav()
        {
            _railNav.Clear();
            _railButtons.Clear();
            _tabbar.Clear();
            _tabButtons.Clear();

            foreach (string route in NavByRole[Role])
            {
                if (!_screens.TryGetValue(route, out ScreenView screen))
                {
                    continue;
                }

                var railButton = Ui.Button(string.Empty, () => Navigate(route), "nav-item");
                railButton.userData = route;
                railButton.Add(Ui.Monogram(screen.Badge, "nav-badge"));
                railButton.Add(Ui.Text(screen.NavLabel, "nav-label"));
                _railButtons.Add((route, railButton));
                _railNav.Add(railButton);

                var tabButton = Ui.Button(string.Empty, () => Navigate(route), "tab-item");
                tabButton.userData = route;
                tabButton.Add(Ui.Monogram(screen.Badge, "tab-badge"));
                tabButton.Add(Ui.Text(screen.NavLabel, "tab-label"));
                _tabButtons.Add((route, tabButton));
                _tabbar.Add(tabButton);
            }
        }

        private void UpdatePersonChrome()
        {
            if (_railName == null)
            {
                return;
            }

            string initials = Initials;
            _railName.text = DisplayName;
            _railEmail.text = Email;
            _railAvatarText.text = initials;
            _wideAvatarText.text = initials;
            _compactAvatarText.text = initials;
            _compactRoleLabel.text = RoleLabel;
        }

        private void UpdateClock()
        {
            if (_clock != null)
            {
                _clock.text = LagosTime.Clock;
            }
        }

        public void ApplyUser(User user)
        {
            if (user == null)
            {
                return;
            }

            _user = user;
            UpdatePersonChrome();
        }

        public void Logout()
        {
            if (_logout != null)
            {
                _ = _logout();
            }
        }

        private static string DefaultRoute(UserRole role)
        {
            return role switch
            {
                UserRole.Operator => "operator",
                UserRole.Rider => "rider",
                _ => "shop"
            };
        }

        public void Navigate(string route)
        {
            if (!_screens.TryGetValue(route, out ScreenView screen))
            {
                Debug.LogWarning($"[ColdRoom] Unknown route '{route}'.");
                return;
            }

            if (Array.IndexOf(NavByRole[Role], route) < 0)
            {
                return;
            }

            _route = route;
            _currentScreen = screen;

            _content.Clear();
            _content.Add(screen.GetView(this));
            _content.scrollOffset = Vector2.zero;

            if (_breadcrumb != null)
            {
                _breadcrumb.text = screen.NavLabel.ToUpperInvariant();
            }

            RefreshActiveNav();
            screen.OnShow();
        }

        private void RefreshActiveNav()
        {
            foreach (var entry in _railButtons)
            {
                entry.Button.EnableInClassList("active", entry.Route == _route);
            }

            foreach (var entry in _tabButtons)
            {
                entry.Button.EnableInClassList("active", entry.Route == _route);
            }
        }

        public void HandleViewport(int width)
        {
            bool compact = width < Viewport.CompactBreakpoint;
            bool narrow = !compact && width < Viewport.NarrowBreakpoint;
            _appRoot.EnableInClassList("compact", compact);
            _appRoot.EnableInClassList("wide", !compact);
            _appRoot.EnableInClassList("narrow", narrow);

            if (_layoutChip != null)
            {
                string mode = compact ? "Compact" : "Wide";
                _layoutChip.text = $"{mode} \u00B7 {width}px";
            }
        }

        public void ShowToast(string message, float seconds = 3.2f)
        {
            if (_toastHost == null)
            {
                return;
            }

            _toastTimer?.Pause();
            _toastLabel.text = message;
            _toastHost.style.display = DisplayStyle.Flex;
            _toastTimer = _toast.schedule.Execute(HideToast).StartingIn((long)(seconds * 1000f));
        }

        private void HideToast()
        {
            if (_toastHost != null)
            {
                _toastHost.style.display = DisplayStyle.None;
            }
        }

        public void RefreshChromeForScreen()
        {
            if (_currentScreen != null && _breadcrumb != null)
            {
                _breadcrumb.text = _currentScreen.NavLabel.ToUpperInvariant();
            }
        }

        private static string InitialsOf(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
            {
                return "?";
            }

            string[] parts = fullName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
            {
                return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpperInvariant();
            }

            return (parts[0][0].ToString() + parts[parts.Length - 1][0]).ToUpperInvariant();
        }
    }
}
