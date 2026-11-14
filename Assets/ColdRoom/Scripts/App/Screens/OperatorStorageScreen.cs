using UnityEngine.UIElements;

namespace ColdRoom.App.Screens
{
    public sealed class OperatorStorageScreen : ScreenView
    {
        public override string Route => "operator-storage";
        public override string NavLabel => "Storage";
        public override string Badge => "ST";
        public override string Eyebrow => "Operator \u00B7 Lagos";
        public override string Title => "Storage approvals";
        public override string Subtitle => "Approve requests, record drop-offs and release goods only after a release request.";

        protected override void Build(VisualElement root)
        {
            root.Add(PageHead());

            var note = Ui.Row("callout", "callout-info");
            note.Add(Ui.Dot("station-dot"));
            note.Add(Ui.Text("Storage is customer-owned goods. It is separate from product orders.", "callout-text"));
            root.Add(note);

            var layout = Ui.Row("two-col");
            var main = Ui.Col("two-col-main");

            main.Add(StorageRow(
                "SR-4K2A",
                "Tola Adeyemi",
                "Requested",
                "2 crates \u00B7 mixed produce",
                "Lekki Phase 1",
                "Today \u00B7 16:20",
                true));
            main.Add(StorageRow(
                "SR-8N1D",
                "Chidi Okafor",
                "Approved",
                "1 box \u00B7 frozen fish",
                "Yaba",
                "Today \u00B7 14:05",
                false));
            main.Add(StorageRow(
                "SR-3P7M",
                "Amaka Obi",
                "Release requested",
                "3 bags \u00B7 assorted vegetables",
                "Ikeja",
                "Yesterday \u00B7 19:40",
                false));

            layout.Add(main);

            var aside = Ui.Col("two-col-aside");
            var states = Ui.Col("card");
            states.Add(Ui.Text("STATES", "card-kicker"));
            states.Add(Ui.Text("Storage transitions", "card-title"));
            states.Add(StateLine("Requested", "Operator reviews the request."));
            states.Add(StateLine("Approved", "Customer can drop off goods."));
            states.Add(StateLine("Stored", "Goods are in the cold room."));
            states.Add(StateLine("Release requested", "Customer asks to collect."));
            states.Add(StateLine("Released", "Goods handed back."));
            aside.Add(states);

            var help = Ui.Col("card", "aside-note");
            help.Add(Ui.Text("GUARD RAILS", "card-kicker"));
            help.Add(Ui.Text("No skipping states", "card-title"));
            help.Add(Ui.Text("Every transition is validated in the data layer. The UI only offers actions that the current state allows.", "card-copy"));
            aside.Add(help);

            layout.Add(aside);
            root.Add(layout);
        }

        private VisualElement StorageRow(string code, string customer, string status, string goods, string station, string arrived, bool actionable)
        {
            var card = Ui.Col("card", "storage-row");
            var top = Ui.Row("queue-top");
            top.Add(Ui.Text(code, "queue-code"));
            top.Add(Ui.Pill(status, status == "Requested" ? "pill-accent" : "pill-soft"));
            top.Add(Ui.Spacer());
            top.Add(Ui.Text(arrived, "queue-age"));
            card.Add(top);

            var body = Ui.Row("queue-body", "wrap");
            body.Add(Ui.Field("Customer", customer, "queue-field"));
            body.Add(Ui.Field("Goods", goods, "queue-field"));
            body.Add(Ui.Field("Station", station, "queue-field"));
            card.Add(body);

            if (actionable)
            {
                var actions = Ui.Row("queue-actions");
                actions.Add(Ui.Primary("Approve request", () => Shell.ShowToast("Storage approvals arrive in step 10.")));
                actions.Add(Ui.Ghost("View details", () => Shell.ShowToast("Storage details arrive in step 10.")));
                card.Add(actions);
            }

            return card;
        }

        private static VisualElement StateLine(string state, string copy)
        {
            var row = Ui.Row("state-line");
            row.Add(Ui.Dot("state-dot"));
            var col = Ui.Col("state-copy");
            col.Add(Ui.Text(state, "state-name"));
            col.Add(Ui.Text(copy, "state-desc"));
            row.Add(col);
            return row;
        }
    }
}
