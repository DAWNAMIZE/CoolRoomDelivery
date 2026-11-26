using System;
using System.Threading.Tasks;
using ColdRoom.Api;
using UnityEngine;
using UnityEngine.UIElements;

namespace ColdRoom.App
{
    /// <summary>Sign in / create account gate shown before the app shell.</summary>
    public sealed class AuthScreen
    {
        private const string DemoPassword = "ColdRoom123!";

        private readonly IColdRoomApi _api;
        private readonly Action<AuthResult> _onAuthenticated;

        private VisualElement _formHost;
        private VisualElement _demoCard;
        private Label _error;
        private Label _title;
        private Label _subtitle;
        private Label _switchText;
        private Button _submit;
        private Button _loginTab;
        private Button _registerTab;
        private Button _switchLink;
        private TextField _fullName;
        private TextField _email;
        private TextField _phone;
        private TextField _password;
        private TextField _confirm;
        private bool _registerMode;
        private bool _busy;

        public AuthScreen(IColdRoomApi api, Action<AuthResult> onAuthenticated)
        {
            _api = api;
            _onAuthenticated = onAuthenticated;
        }

        public void Build(VisualElement host)
        {
            host.Clear();
            host.AddToClassList("app");
            host.AddToClassList("auth-page");

            var scroll = new ScrollView(ScrollViewMode.Vertical)
            {
                horizontalScrollerVisibility = ScrollerVisibility.Hidden,
                verticalScrollerVisibility = ScrollerVisibility.Hidden,
                touchScrollBehavior = ScrollView.TouchScrollBehavior.Clamped
            };
            scroll.AddToClassList("auth-scroll");
            host.Add(scroll);

            var shell = Ui.Row("auth-shell");
            shell.Add(BuildAside());
            shell.Add(BuildMain());
            scroll.Add(shell);

            BuildForm();
        }

        private VisualElement BuildAside()
        {
            var aside = Ui.Col("auth-aside");

            var brand = Ui.Row("auth-brand-row");
            brand.Add(Ui.Monogram("CS", "brand-mark"));
            brand.Add(Ui.Text("CamaanStores", "auth-brand-name"));
            aside.Add(brand);

            aside.Add(Ui.Text("LAGOS COLD-CHAIN", "section-kicker"));
            aside.Add(Ui.Text("Cold chain,\nwithout the chaos.", "auth-aside-title"));
            aside.Add(Ui.Text(
                "Chilled protein and produce, held at temperature and collected from a station near you.",
                "auth-aside-copy"));

            aside.Add(Feature("One account, one role \u2014 customers, operators and riders."));
            aside.Add(Feature("Live stock, urgency and order state from a single API."));
            aside.Add(Feature("Your session survives a page reload."));

            var stations = Ui.Row("auth-station-row", "wrap");
            stations.Add(Ui.Pill("Lekki Phase 1", "pill-soft"));
            stations.Add(Ui.Pill("Yaba", "pill-soft"));
            stations.Add(Ui.Pill("Ikeja", "pill-soft"));
            stations.Add(Ui.Pill("Surulere", "pill-soft"));
            aside.Add(stations);

            return aside;
        }

        private VisualElement BuildMain()
        {
            var main = Ui.Col("auth-main");

            var compactBrand = Ui.Row("auth-compact-brand");
            compactBrand.Add(Ui.Monogram("CS", "brand-mark-sm"));
            compactBrand.Add(Ui.Text("CamaanStores", "compact-brand-name"));
            main.Add(compactBrand);

            main.Add(Ui.Text("WELCOME", "section-kicker"));
            _title = Ui.Text("Sign in to your cold room", "auth-title");
            main.Add(_title);
            _subtitle = Ui.Text("Use a seeded demo account or create a customer account.", "auth-sub");
            main.Add(_subtitle);

            var tabs = Ui.Row("auth-tabs");
            _loginTab = Ui.Button("Sign in", () => SetMode(false), "auth-tab", "active");
            _registerTab = Ui.Button("Create account", () => SetMode(true), "auth-tab");
            tabs.Add(_loginTab);
            tabs.Add(_registerTab);
            main.Add(tabs);

            _formHost = Ui.Col("auth-form");
            main.Add(_formHost);

            _error = Ui.Text(string.Empty, "auth-error");
            _error.style.display = DisplayStyle.None;
            main.Add(_error);

            _submit = Ui.Primary("Sign in", Submit);
            _submit.AddToClassList("auth-submit");
            main.Add(_submit);

            var modeRow = Ui.Row("auth-switch");
            _switchText = Ui.Text("New to CamaanStores?", "auth-switch-text");
            _switchLink = Ui.Button("Create an account", () => SetMode(!_registerMode), "auth-link");
            modeRow.Add(_switchText);
            modeRow.Add(_switchLink);
            main.Add(modeRow);

            _demoCard = BuildDemo();
            main.Add(_demoCard);

            return main;
        }

        private VisualElement BuildDemo()
        {
            var demo = Ui.Col("auth-demo");
            demo.Add(Ui.Text("DEMO ACCOUNTS", "card-kicker"));
            demo.Add(DemoRow("Customer", "customer@coldroom.test"));
            demo.Add(DemoRow("Operator", "operator@coldroom.test"));
            demo.Add(DemoRow("Rider", "rider@coldroom.test"));

            var password = Ui.Row("auth-demo-row");
            password.Add(Ui.Text("Password", "auth-demo-role"));
            password.Add(Ui.Text(DemoPassword, "auth-demo-pass"));
            demo.Add(password);
            demo.Add(Ui.Text("Click a row to fill the form. New accounts are customers only.", "auth-note"));
            return demo;
        }

        private VisualElement DemoRow(string role, string email)
        {
            var row = Ui.Row("auth-demo-row");
            row.Add(Ui.Text(role, "auth-demo-role"));
            row.Add(Ui.Text(email, "auth-demo-email"));
            row.Add(Ui.Spacer());
            row.Add(Ui.Text("Use", "auth-demo-use"));
            Ui.Clickable(row, () =>
            {
                _email.value = email;
                _password.value = DemoPassword;
                HideError();
                _submit.Focus();
            });
            return row;
        }

        private void SetMode(bool register)
        {
            if (_registerMode == register)
            {
                return;
            }

            _registerMode = register;
            _loginTab.EnableInClassList("active", !register);
            _registerTab.EnableInClassList("active", register);
            _title.text = register ? "Create your customer account" : "Sign in to your cold room";
            _subtitle.text = register
                ? "New accounts are customers. Staff accounts are provisioned by the operator."
                : "Use a seeded demo account or create a customer account.";
            _switchText.text = register ? "Already have an account?" : "New to CamaanStores?";
            _switchLink.text = register ? "Sign in" : "Create an account";
            _demoCard.style.display = register ? DisplayStyle.None : DisplayStyle.Flex;
            _submit.text = register ? "Create account" : "Sign in";
            HideError();
            BuildForm();
        }

        private void BuildForm()
        {
            _formHost.Clear();

            if (_registerMode)
            {
                _fullName = Input("FULL NAME");
                _email = Input("EMAIL");
                _phone = Input("PHONE");
                _password = Input("PASSWORD", true);
                _confirm = Input("CONFIRM PASSWORD", true);
                _formHost.Add(_fullName);
                _formHost.Add(_email);
                _formHost.Add(_phone);
                _formHost.Add(_password);
                _formHost.Add(_confirm);
            }
            else
            {
                _email = Input("EMAIL");
                _password = Input("PASSWORD", true);
                _formHost.Add(_email);
                _formHost.Add(_password);
            }
        }

        private TextField Input(string label, bool password = false)
        {
            var field = new TextField(label) { isPasswordField = password };
            field.AddToClassList("auth-input");
            field.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                {
                    Submit();
                    evt.StopPropagation();
                }
            });
            return field;
        }

        private void Submit()
        {
            if (!_busy)
            {
                _ = SubmitAsync();
            }
        }

        private async Task SubmitAsync()
        {
            HideError();

            string email = (_email.value ?? string.Empty).Trim();
            string password = _password.value ?? string.Empty;

            if (!IsValidEmail(email))
            {
                ShowError("Enter a valid email address.");
                return;
            }

            if (password.Length < 8)
            {
                ShowError("Password must be at least 8 characters.");
                return;
            }

            string fullName = string.Empty;
            string phone = string.Empty;

            if (_registerMode)
            {
                fullName = (_fullName.value ?? string.Empty).Trim();
                phone = (_phone.value ?? string.Empty).Trim();

                if (fullName.Length < 2)
                {
                    ShowError("Enter your full name.");
                    return;
                }

                if (!IsValidPhone(phone))
                {
                    ShowError("Enter a valid phone number, e.g. +234 801 234 5678.");
                    return;
                }

                if (password != (_confirm.value ?? string.Empty))
                {
                    ShowError("The two passwords do not match.");
                    return;
                }
            }

            SetBusy(true);

            try
            {
                AuthResult result = _registerMode
                    ? await _api.Register(email, password, fullName, phone)
                    : await _api.Login(email, password);

                _onAuthenticated?.Invoke(result);
            }
            catch (ApiException exception)
            {
                ShowError(Friendly(exception));
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[ColdRoom] Sign-in failed: {exception.Message}");
                ShowError("Something went wrong. Please try again.");
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void SetBusy(bool busy)
        {
            _busy = busy;
            _submit.SetEnabled(!busy);
            _submit.text = busy
                ? (_registerMode ? "Creating account\u2026" : "Signing in\u2026")
                : (_registerMode ? "Create account" : "Sign in");
        }

        private void ShowError(string message)
        {
            _error.text = message;
            _error.style.display = DisplayStyle.Flex;
        }

        private void HideError()
        {
            if (_error != null)
            {
                _error.style.display = DisplayStyle.None;
            }
        }

        private static string Friendly(ApiException exception)
        {
            return exception.Code switch
            {
                ApiErrorCodes.Unauthorized => "Email or password is incorrect.",
                ApiErrorCodes.EmailTaken => "An account with that email already exists. Try signing in.",
                _ => exception.Message
            };
        }

        private static bool IsValidEmail(string email)
        {
            int at = email.IndexOf('@');
            int dot = email.LastIndexOf('.');
            return at > 0 && dot > at + 1 && dot < email.Length - 1 && !email.Contains(" ");
        }

        private static bool IsValidPhone(string phone)
        {
            int digits = 0;
            foreach (char character in phone)
            {
                if (char.IsDigit(character))
                {
                    digits++;
                }
                else if (character != '+' && character != ' ' && character != '-')
                {
                    return false;
                }
            }

            return digits >= 10 && digits <= 15;
        }

        private static VisualElement Feature(string text)
        {
            var row = Ui.Row("auth-feature");
            row.Add(Ui.Dot("auth-feature-dot"));
            row.Add(Ui.Text(text, "auth-feature-text"));
            return row;
        }
    }
}
