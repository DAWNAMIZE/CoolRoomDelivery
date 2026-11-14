using System;
using System.Collections.Generic;
using System.Globalization;
using ColdRoom.App.Screens;
using ColdRoom.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace ColdRoom.App
{
    public sealed class RolePersona
    {
        public RolePersona(UserRole role, string name, string email, string initials)
        {
            Role = role;
            Name = name;
            Email = email;
            Initials = initials;
        }

        public UserRole Role { get; }
        public string Name { get; }
        public string Email { get; }
        public string Initials { get; }
    }

    public sealed class AppShell
    {
        private static readonly Dictionary<UserRole, string[]> NavByRole = new()
        {
            { UserRole.Customer, new[] { "shop", "cart", "orders", "storage", "profile" } },
            { UserRole.Operator, new[] { "operator", "operator-storage", "profile" } },
            { UserRole.Rider, new[] { "rider", "profile" } }
        };

        private static readonly Dictionary<UserRole, RolePersona> Personas = new()
        {
            { UserRole.Customer, new RolePersona(UserRole.Customer, "Tola Adeyemi", "customer@coldroom.test", "TA") },
            { UserRole.Operator, new RolePersona(UserRole.Operator, "Ngozi Eze", "operator@coldroom.test", "NE") },
            { UserRole.Rider, new RolePersona(UserRole.Rider, "Seye Ogun", "rider@coldroom.test", "SO") }
        };

        private readonly ColdRoomApp _app;
        private readonly VisualElement _appRoot;
        private readonly Dictionary<string, ScreenView> _screens = new();
        private readonly List<(UserRole Role, Button Button)> _roleButtons = new();
        private readonly List<(string Route, Button Button)> _railButtons = new();
        private readonly List<(string Route, Button Button)> _tabButtons = new();

        private VisualElement _shell;
        private VisualElement _railNav;
        private VisualElement _roleSwitchHost;
        private VisualElement _tabbar;
        private ScrollView _content;
        private Button _compactRoleButton;
        private Label _breadcrumb;
        private Label _clock;
        private Label _railName;
        private Label _railEmail;
        private Label _railAvatarText;
        private Label _wideAvatarText;
        private Label _compactAvatarText;
        private Label _layoutChip;
        private Label _toastLabel;
        private VisualElement _toastHost;
        private VisualElement _toast;
        private IVisualElementScheduledItem _toastTimer;
        private UserRole _role = UserRole.Customer;
        private string _route;
        private ScreenView _currentScreen;

        public AppShell(ColdRoomApp app, VisualElement appRoot)
        {
            _app = app;
            _appRoot = appRoot;
        }

        public UserRole Role => _role;
        public RolePersona Persona => Personas[_role];
        public string RoleLabel => _role.ToString();
        public string CurrentRoute => _route;

        public void Build()
        {
            _appRoot.Clear();
            _appRoot.AddToClassList("app");

            RegisterScreens();

            _shell = Ui.El("shell");
            _appRoot.Add(_shell);

            _shell.Add(BuildRail());
            _shell.Add(BuildMain());

            BuildToast();

            _shell.schedule.Execute(UpdateClock).Every(1000);
            UpdatePersonaChrome();
            SetRole(_role, notify: false);
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
            brand.Add(Ui.Monogram("CR", "brand-mark"));
            var brandCopy = Ui.Col("brand-copy");
            brandCopy.Add(Ui.Text("ColdRoom", "brand-name"));
            brandCopy.Add(Ui.Text("Lagos cold-chain", "brand-sub"));
            brand.Add(brandCopy);
            rail.Add(brand);

            var roleBlock = Ui.Col("role-block");
            roleBlock.Add(Ui.Text("PREVIEW AS", "rail-label"));
            _roleSwitchHost = Ui.Row("role-switch");
            roleBlock.Add(_roleSwitchHost);
            rail.Add(roleBlock);

            _railNav = Ui.Col("rail-nav");
            rail.Add(_railNav);

            rail.Add(Ui.Spacer());

            var person = Ui.Row("rail-person");
            var avatar = Ui.Avatar(Persona.Initials, "avatar-sm");
            _railAvatarText = avatar.Q<Label>();
            person.Add(avatar);
            var personCopy = Ui.Col("rail-person-copy");
            _railName = Ui.Text(Persona.Name, "rail-person-name");
            _railEmail = Ui.Text(Persona.Email, "rail-person-email");
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
            footer.Add(Ui.Text("Step 1 of 11 \u00B7 App shell", "rail-footer-sub"));
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
            bar.Add(Ui.Text("COLDROOM", "breadcrumb-root"));
            bar.Add(Ui.Text("/", "breadcrumb-sep"));
            bar.Add(_breadcrumb);

            bar.Add(Ui.El("topbar-sep"));

            var clockCopy = Ui.Col("clock-copy");
            clockCopy.Add(Ui.Text("LAGOS", "clock-label"));
            _clock = Ui.Text(LagosTime.Clock, "clock");
            clockCopy.Add(_clock);
            bar.Add(clockCopy);

            var avatar = Ui.Avatar(Persona.Initials, "avatar-sm", "topbar-avatar");
            _wideAvatarText = avatar.Q<Label>();
            Ui.Clickable(avatar, () => Navigate("profile"));
            bar.Add(avatar);

            return bar;
        }

        private VisualElement BuildCompactTopbar()
        {
            var bar = Ui.Row("topbar", "topbar-compact");

            var brand = Ui.Row("compact-brand");
            brand.Add(Ui.Monogram("CR", "brand-mark-sm"));
            brand.Add(Ui.Text("ColdRoom", "compact-brand-name"));
            bar.Add(brand);

            bar.Add(Ui.Spacer());

            _compactRoleButton = Ui.Button(Persona.Role.ToString(), CycleRole, "role-pill");
            _compactRoleButton.tooltip = "Tap to preview another role";
            bar.Add(_compactRoleButton);

            var avatar = Ui.Avatar(Persona.Initials, "avatar-xs");
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

        private void RebuildRoleSwitch()
        {
            _roleSwitchHost.Clear();
            _roleButtons.Clear();

            foreach (UserRole role in new[] { UserRole.Customer, UserRole.Operator, UserRole.Rider })
            {
                var button = Ui.Button(role.ToString(), () => SetRole(role), "seg", "seg-small");
                button.userData = role;
                _roleButtons.Add((role, button));
                _roleSwitchHost.Add(button);
            }

            RefreshRoleButtons();
        }

        private void RefreshRoleButtons()
        {
            foreach (var entry in _roleButtons)
            {
                entry.Button.EnableInClassList("active", entry.Role == _role);
            }

            if (_compactRoleButton != null)
            {
                _compactRoleButton.text = Persona.Role.ToString();
            }
        }

        private void RebuildRailNav()
        {
            _railNav.Clear();
            _railButtons.Clear();

            foreach (string route in NavByRole[_role])
            {
                if (!_screens.TryGetValue(route, out ScreenView screen))
                {
                    continue;
                }

                var button = Ui.Button(string.Empty, () => Navigate(route), "nav-item");
                button.userData = route;
                var badge = Ui.Monogram(screen.Badge, "nav-badge");
                button.Add(badge);
                button.Add(Ui.Text(screen.NavLabel, "nav-label"));
                _railButtons.Add((route, button));
                _railNav.Add(button);
            }
        }

        private void RebuildTabbar()
        {
            _tabbar.Clear();
            _tabButtons.Clear();

            foreach (string route in NavByRole[_role])
            {
                if (!_screens.TryGetValue(route, out ScreenView screen))
                {
                    continue;
                }

                var button = Ui.Button(string.Empty, () => Navigate(route), "tab-item");
                button.userData = route;
                button.Add(Ui.Monogram(screen.Badge, "tab-badge"));
                button.Add(Ui.Text(screen.NavLabel, "tab-label"));
                _tabButtons.Add((route, button));
                _tabbar.Add(button);
            }
        }

        private void UpdatePersonaChrome()
        {
            var persona = Persona;
            if (_railName != null)
            {
                _railName.text = persona.Name;
                _railEmail.text = persona.Email;
                _railAvatarText.text = persona.Initials;
                _wideAvatarText.text = persona.Initials;
                _compactAvatarText.text = persona.Initials;
            }

            RefreshRoleButtons();
        }

        private void UpdateClock()
        {
            if (_clock != null)
            {
                _clock.text = LagosTime.Clock;
            }
        }

        public void SetRole(UserRole role, bool notify = true)
        {
            if (!Personas.ContainsKey(role))
            {
                return;
            }

            bool changed = _role != role;
            _role = role;

            RebuildRoleSwitch();
            RebuildRailNav();
            RebuildTabbar();
            UpdatePersonaChrome();

            string defaultRoute = DefaultRoute(role);
            bool routeStillValid = Array.IndexOf(NavByRole[role], _route) >= 0;

            if (!routeStillValid || changed)
            {
                Navigate(defaultRoute);
            }
            else
            {
                RefreshActiveNav();
            }

            if (notify && changed)
            {
                ShowToast($"Previewing the {role} experience");
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

        private void CycleRole()
        {
            switch (_role)
            {
                case UserRole.Customer:
                    SetRole(UserRole.Operator);
                    break;
                case UserRole.Operator:
                    SetRole(UserRole.Rider);
                    break;
                default:
                    SetRole(UserRole.Customer);
                    break;
            }
        }

        public void Navigate(string route)
        {
            if (!_screens.TryGetValue(route, out ScreenView screen))
            {
                Debug.LogWarning($"[ColdRoom] Unknown route '{route}'.");
                return;
            }

            if (Array.IndexOf(NavByRole[_role], route) < 0)
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
    }
}
