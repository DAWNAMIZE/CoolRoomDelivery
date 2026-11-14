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

        private Label _nameLabel;
        private Label _emailLabel;
        private Label _avatarLabel;
        private VisualElement _roleLabel;
        private Label _stationValue;
        private Label _phoneValue;

        protected override void Build(VisualElement root)
        {
            root.Add(PageHead());

            var layout = Ui.Row("two-col");
            var main = Ui.Col("two-col-main");

            var card = Ui.Col("card", "profile-card");
            var header = Ui.Row("profile-header");
            var avatar = Ui.Avatar("TA", "avatar-lg");
            _avatarLabel = avatar.Q<Label>();
            header.Add(avatar);
            var identity = Ui.Col("profile-identity");
            _nameLabel = Ui.Text("Tola Adeyemi", "profile-name");
            _emailLabel = Ui.Text("customer@coldroom.test", "profile-email");
            var meta = Ui.Row("profile-meta");
            _roleLabel = Ui.Pill("Customer", "pill-accent");
            meta.Add(_roleLabel);
            meta.Add(Ui.Pill("Preview session", "pill-soft"));
            identity.Add(_nameLabel);
            identity.Add(_emailLabel);
            identity.Add(meta);
            header.Add(identity);
            header.Add(Ui.Spacer());
            card.Add(header);

            card.Add(Ui.Divider());

            var fields = Ui.Row("profile-fields", "wrap");
            fields.Add(Ui.Field("Full name", "Tola Adeyemi", "profile-field"));
            _phoneValue = Ui.Text("+234 801 234 5678", "field-value");
            var phoneField = Ui.Col("field", "profile-field");
            phoneField.Add(Ui.Text("PHONE", "field-label"));
            phoneField.Add(_phoneValue);
            fields.Add(phoneField);
            fields.Add(Ui.Field("Email", "customer@coldroom.test", "profile-field"));
            _stationValue = Ui.Text("Lekki Phase 1", "field-value");
            var stationField = Ui.Col("field", "profile-field");
            stationField.Add(Ui.Text("DEFAULT PICKUP STATION", "field-label"));
            stationField.Add(_stationValue);
            fields.Add(stationField);
            card.Add(fields);

            var actions = Ui.Row("button-row");
            var edit = Ui.Primary("Edit profile", () => Shell.ShowToast("Profile editing arrives in step 2."));
            actions.Add(edit);
            actions.Add(Ui.Secondary("Change station", () => Shell.ShowToast("Pickup stations are editable in step 2.")));
            card.Add(actions);

            main.Add(card);

            var security = Ui.Col("card");
            security.Add(Ui.Text("ACCOUNT RULES", "card-kicker"));
            security.Add(Ui.Text("One account, one role", "card-title"));
            security.Add(Ui.Text(
                "Customers order and book storage. Operators run the queue and approve storage. Riders move assigned orders through transit.",
                "card-copy"));
            security.Add(Ui.Text(
                "Email is unique. Phone numbers are stored in international format. The default pickup station is one of the four Lagos stations.",
                "card-copy"));
            main.Add(security);

            layout.Add(main);

            var aside = Ui.Col("two-col-aside");
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
            demo.Add(Ui.Text("Real sign-in is wired up in step 2. This screen is a preview of the customer profile.", "fine-print"));
            aside.Add(demo);

            var prefs = Ui.Col("card");
            prefs.Add(Ui.Text("NOTIFICATIONS", "card-kicker"));
            prefs.Add(Ui.Text("In-app only", "card-title"));
            prefs.Add(Ui.Text("No SMS or push notifications in this MVP. Order updates appear in the app.", "card-copy"));
            aside.Add(prefs);

            layout.Add(aside);
            root.Add(layout);

            Refresh();
        }

        public override void OnShow()
        {
            Refresh();
        }

        private void Refresh()
        {
            var persona = Shell.Persona;
            if (_nameLabel == null)
            {
                return;
            }

            _nameLabel.text = persona.Name;
            _emailLabel.text = persona.Email;
            _roleLabel.Q<Label>().text = persona.Role.ToString();
            _avatarLabel.text = persona.Initials;
            _stationValue.text = persona.Role == ColdRoom.Core.UserRole.Customer ? "Lekki Phase 1" : "Staff \u00B7 no station";
            _phoneValue.text = persona.Role == ColdRoom.Core.UserRole.Customer ? "+234 801 234 5678" : "Not set";
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
