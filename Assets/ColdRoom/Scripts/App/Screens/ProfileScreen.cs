using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ColdRoom.Api;
using ColdRoom.Core;
using ColdRoom.Core.Models;
using UnityEngine;
using UnityEngine.UIElements;

namespace ColdRoom.App.Screens
{
    public sealed class ProfileScreen : ScreenView
    {
        public override string Route => "profile";
        public override string NavLabel => "Profile";
        public override string Badge => "ME";
        public override string Eyebrow => "Account";
        public override string Title => "Profile";
        public override string Subtitle => "Contact details, role and default pickup preferences.";

        private readonly List<PickupStation> _stations = new();

        private VisualElement _host;
        private TextField _editName;
        private TextField _editPhone;
        private DropdownField _editStation;
        private Label _editError;
        private User _user;
        private bool _loaded;
        private bool _loading;
        private bool _editing;
        private bool _saving;

        protected override void Build(VisualElement root)
        {
            root.Add(PageHead());

            var layout = Ui.Row("two-col");
            var main = Ui.Col("two-col-main");
            _host = Ui.Col("profile-host");
            _host.Add(LoadingState("Loading your profile\u2026"));
            main.Add(_host);
            layout.Add(main);

            var aside = Ui.Col("two-col-aside");
            aside.Add(BuildDemoCard());
            aside.Add(BuildNotificationsCard());
            layout.Add(aside);

            root.Add(layout);
        }

        public override void OnShow()
        {
            if (!_loaded && !_loading)
            {
                _ = LoadAsync();
            }
        }

        private async Task LoadAsync()
        {
            _loading = true;
            _host.Clear();
            _host.Add(LoadingState("Loading your profile\u2026"));

            try
            {
                Task<User> meTask = Shell.Api.GetMe();
                Task<IReadOnlyList<PickupStation>> stationsTask = Shell.Api.GetStations();
                await Task.WhenAll(meTask, stationsTask);

                _user = meTask.Result;
                _stations.Clear();
                _stations.AddRange(stationsTask.Result);
                Shell.ApplyUser(_user);
                _loaded = true;
                Render();
            }
            catch (ApiException exception)
            {
                ShowLoadError(exception.Message);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[ColdRoom] Profile load failed: {exception.Message}");
                ShowLoadError("Your profile could not be loaded.");
            }
            finally
            {
                _loading = false;
            }
        }

        private void ShowLoadError(string message)
        {
            _host.Clear();
            _host.Add(ErrorState("Profile unavailable", message, () => _ = LoadAsync()));
        }

        private void Render()
        {
            if (_user == null)
            {
                return;
            }

            _host.Clear();

            var card = Ui.Col("card", "profile-card");
            var header = Ui.Row("profile-header");
            var avatar = Ui.Avatar(Shell.Initials, "avatar-lg");
            header.Add(avatar);

            var identity = Ui.Col("profile-identity");
            identity.Add(Ui.Text(_user.FullName, "profile-name"));
            identity.Add(Ui.Text(_user.Email, "profile-email"));

            var meta = Ui.Row("profile-meta");
            meta.Add(Ui.Pill(_user.Role.ToString(), "pill-accent"));
            meta.Add(Ui.Pill("Signed in", "pill-soft"));
            identity.Add(meta);

            header.Add(identity);
            header.Add(Ui.Spacer());
            card.Add(header);
            card.Add(Ui.Divider());

            var fields = Ui.Row("profile-fields", "wrap");
            fields.Add(Ui.Field("Full name", _user.FullName, "profile-field"));

            var phoneField = Ui.Col("field", "profile-field");
            phoneField.Add(Ui.Text("PHONE", "field-label"));
            phoneField.Add(Ui.Text(string.IsNullOrWhiteSpace(_user.Phone) ? "Not set" : _user.Phone, "field-value"));
            fields.Add(phoneField);

            fields.Add(Ui.Field("Email", _user.Email, "profile-field"));

            var stationField = Ui.Col("field", "profile-field");
            stationField.Add(Ui.Text("DEFAULT PICKUP STATION", "field-label"));
            stationField.Add(Ui.Text(StationName(), "field-value"));
            fields.Add(stationField);
            card.Add(fields);

            if (_editing)
            {
                card.Add(BuildEditForm());
            }

            var actions = Ui.Row("button-row");
            if (_editing)
            {
                actions.Add(Ui.Primary("Save changes", () => _ = SaveAsync()));
                actions.Add(Ui.Ghost("Cancel", () =>
                {
                    _editing = false;
                    Render();
                }));
            }
            else
            {
                actions.Add(Ui.Primary("Edit profile", () =>
                {
                    _editing = true;
                    Render();
                }));
                actions.Add(Ui.Button("Log out", Shell.Logout, "btn", "btn-secondary", "logout-btn"));
            }

            card.Add(actions);
            _host.Add(card);
            _host.Add(BuildRulesCard());
        }

        private VisualElement BuildEditForm()
        {
            var form = Ui.Col("edit-form");
            form.Add(Ui.Text("EDIT PROFILE", "card-kicker"));

            _editName = EditInput("FULL NAME", _user.FullName);
            _editPhone = EditInput("PHONE", _user.Phone);
            form.Add(_editName);
            form.Add(_editPhone);

            var stationNames = new List<string>();
            if (Shell.Role != UserRole.Customer)
            {
                stationNames.Add("No station");
            }

            stationNames.AddRange(_stations.Select(s => s.Name));

            int index = 0;
            int stationIndex = _stations.FindIndex(s => s.Id == _user.DefaultStationId);
            if (Shell.Role != UserRole.Customer)
            {
                index = stationIndex < 0 ? 0 : stationIndex + 1;
            }
            else if (stationIndex >= 0)
            {
                index = stationIndex;
            }

            _editStation = new DropdownField("DEFAULT PICKUP STATION", stationNames, index);
            _editStation.AddToClassList("auth-input");
            form.Add(_editStation);

            _editError = Ui.Text(string.Empty, "auth-error");
            _editError.style.display = DisplayStyle.None;
            form.Add(_editError);

            return form;
        }

        private async Task SaveAsync()
        {
            if (_saving)
            {
                return;
            }

            _saving = true;
            HideEditError();

            try
            {
                _user = await Shell.Api.UpdateMe(
                    (_editName.value ?? string.Empty).Trim(),
                    (_editPhone.value ?? string.Empty).Trim(),
                    SelectedStationId());

                Shell.ApplyUser(_user);
                _editing = false;
                Render();
                Shell.ShowToast("Profile updated.");
            }
            catch (ApiException exception)
            {
                ShowEditError(exception.Message);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[ColdRoom] Profile update failed: {exception.Message}");
                ShowEditError("Could not save your profile. Try again.");
            }
            finally
            {
                _saving = false;
            }
        }

        private string SelectedStationId()
        {
            if (_stations.Count == 0)
            {
                return null;
            }

            int index = _editStation.index;
            if (Shell.Role != UserRole.Customer)
            {
                if (index <= 0)
                {
                    return null;
                }

                index--;
            }

            if (index < 0 || index >= _stations.Count)
            {
                return Shell.Role == UserRole.Customer ? _stations[0].Id : null;
            }

            return _stations[index].Id;
        }

        private string StationName()
        {
            PickupStation station = _stations.FirstOrDefault(s => s.Id == _user.DefaultStationId);
            if (station != null)
            {
                return station.Name;
            }

            return _user.Role == UserRole.Customer ? "Not set" : "Staff \u00B7 no station";
        }

        private void ShowEditError(string message)
        {
            if (_editError != null)
            {
                _editError.text = message;
                _editError.style.display = DisplayStyle.Flex;
            }
        }

        private void HideEditError()
        {
            if (_editError != null)
            {
                _editError.style.display = DisplayStyle.None;
            }
        }

        private VisualElement BuildRulesCard()
        {
            var security = Ui.Col("card");
            security.Add(Ui.Text("ACCOUNT RULES", "card-kicker"));
            security.Add(Ui.Text("One account, one role", "card-title"));
            security.Add(Ui.Text(
                "Customers order and book storage. Operators run the queue and approve storage. Riders move assigned orders through transit.",
                "card-copy"));
            security.Add(Ui.Text(
                "Email is unique. Phone numbers are stored in international format. The default pickup station is one of the four Lagos stations.",
                "card-copy"));
            security.Add(Ui.Text(
                "Your session is stored locally and restored on reload. Logging out revokes it.",
                "card-copy"));
            return security;
        }

        private static VisualElement BuildDemoCard()
        {
            var demo = Ui.Col("card", "demo-card");
            demo.Add(Ui.Text("DEMO ACCOUNTS", "card-kicker"));
            demo.Add(Ui.Text("Seeded for testing", "card-title"));
            demo.Add(DemoRow("Customer", "customer@coldroom.test"));
            demo.Add(DemoRow("Operator", "operator@coldroom.test"));
            demo.Add(DemoRow("Rider", "rider@coldroom.test"));
            demo.Add(Ui.Divider());

            var password = Ui.Row("password-row");
            password.Add(Ui.Text("Password", "field-label"));
            password.Add(Ui.Spacer());
            password.Add(Ui.Text("ColdRoom123!", "password-value"));
            demo.Add(password);
            demo.Add(Ui.Text("These accounts are created by the mock API on first run.", "fine-print"));
            return demo;
        }

        private static VisualElement BuildNotificationsCard()
        {
            var prefs = Ui.Col("card");
            prefs.Add(Ui.Text("NOTIFICATIONS", "card-kicker"));
            prefs.Add(Ui.Text("In-app only", "card-title"));
            prefs.Add(Ui.Text("No SMS or push notifications in this MVP. Order updates appear in the app.", "card-copy"));
            return prefs;
        }

        private static TextField EditInput(string label, string value)
        {
            var field = new TextField(label) { value = value ?? string.Empty };
            field.AddToClassList("auth-input");
            return field;
        }

        private static VisualElement DemoRow(string role, string email)
        {
            var row = Ui.Row("demo-account");
            row.Add(Ui.Monogram(role.Substring(0, 2).ToUpperInvariant(), "demo-mark"));
            var copy = Ui.Col("demo-copy");
            copy.Add(Ui.Text(role, "demo-role"));
            copy.Add(Ui.Text(email, "demo-email"));
            row.Add(copy);
            return row;
        }
    }
}
