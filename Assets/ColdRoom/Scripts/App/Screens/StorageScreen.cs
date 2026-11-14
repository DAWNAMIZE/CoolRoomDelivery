using UnityEngine.UIElements;

namespace ColdRoom.App.Screens
{
    public sealed class StorageScreen : ScreenView
    {
        public override string Route => "storage";
        public override string NavLabel => "Storage";
        public override string Badge => "ST";
        public override string Eyebrow => "Customer";
        public override string Title => "Cold storage";
        public override string Subtitle => "Send your own goods into the cold room and track every hand-off.";

        protected override void Build(VisualElement root)
        {
            root.Add(PageHead());

            var layout = Ui.Row("two-col");
            var main = Ui.Col("two-col-main");

            var request = Ui.Col("card", "storage-card");
            var top = Ui.Row("storage-top");
            top.Add(Ui.Monogram("ST", "role-mark"));
            top.Add(Ui.Spacer());
            top.Add(Ui.Pill("New request", "pill-accent"));
            request.Add(top);
            request.Add(Ui.Text("Store goods with ColdRoom", "card-title", "card-title-lg"));
            request.Add(Ui.Text(
                "Tell us what you are storing and which station you'll use. We approve the request, receive the goods, and keep them at temperature until you ask for release.",
                "card-copy"));
            var requestButton = Ui.Primary("Start a storage request", () =>
                Shell.ShowToast("Storage requests are wired up in step 10."));
            request.Add(requestButton);
            request.Add(Ui.Text("No payment is collected in this MVP.", "fine-print"));
            main.Add(request);

            var flow = Ui.Col("section");
            flow.Add(Ui.SectionHead("Storage lifecycle", "Five states"));
            var flowCard = Ui.Col("card", "pipeline-card");
            var row = Ui.Row("storage-flow", "wrap");
            string[] stages = { "Requested", "Approved", "Stored", "Release requested", "Released" };
            for (int i = 0; i < stages.Length; i++)
            {
                var stage = Ui.Col("storage-stage");
                stage.Add(Ui.Text((i + 1).ToString("00"), "pipeline-number"));
                stage.Add(Ui.Text(stages[i], "storage-stage-name"));
                row.Add(stage);
                if (i < stages.Length - 1)
                {
                    row.Add(Ui.Text(">", "pipeline-arrow"));
                }
            }

            flowCard.Add(row);
            flowCard.Add(Ui.Text("A request only becomes Stored after an operator approves it and records the drop-off.", "card-copy"));
            flow.Add(flowCard);
            main.Add(flow);

            layout.Add(main);

            var aside = Ui.Col("two-col-aside");
            var stations = Ui.Col("card");
            stations.Add(Ui.Text("PICKUP STATIONS", "card-kicker"));
            stations.Add(Ui.Text("Four points across Lagos", "card-title"));
            stations.Add(StationRow("Lekki Phase 1", "Island \u00B7 06:00\u201322:00"));
            stations.Add(StationRow("Yaba", "Mainland \u00B7 06:00\u201322:00"));
            stations.Add(StationRow("Ikeja", "Mainland \u00B7 06:00\u201322:00"));
            stations.Add(StationRow("Surulere", "Mainland \u00B7 06:00\u201322:00"));
            aside.Add(stations);

            var capacity = Ui.Col("card", "aside-note");
            capacity.Add(Ui.Text("CAPACITY", "card-kicker"));
            capacity.Add(Ui.Text("6 bays available", "card-title"));
            capacity.Add(Ui.Text("Storage is held between 2\u20134\u00B0C and monitored by the operator on duty.", "card-copy"));
            aside.Add(capacity);

            layout.Add(aside);
            root.Add(layout);
        }

        private static VisualElement StationRow(string name, string meta)
        {
            var row = Ui.Row("station-row");
            row.Add(Ui.Dot("station-dot"));
            var col = Ui.Col("station-copy");
            col.Add(Ui.Text(name, "station-name"));
            col.Add(Ui.Text(meta, "station-meta"));
            row.Add(col);
            return row;
        }
    }
}
