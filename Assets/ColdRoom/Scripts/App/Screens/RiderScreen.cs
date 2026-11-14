using UnityEngine.UIElements;

namespace ColdRoom.App.Screens
{
    public sealed class RiderScreen : ScreenView
    {
        public override string Route => "rider";
        public override string NavLabel => "Deliveries";
        public override string Badge => "RD";
        public override string Eyebrow => "Rider \u00B7 Lagos";
        public override string Title => "Deliveries";
        public override string Subtitle => "Your assigned runs and the barcode you present at the pickup station.";

        protected override void Build(VisualElement root)
        {
            root.Add(PageHead());

            var layout = Ui.Row("two-col");
            var main = Ui.Col("two-col-main");

            var assigned = Ui.Col("card", "delivery-card");
            var top = Ui.Row("queue-top");
            top.Add(Ui.Text("ASSIGNED RUN", "card-kicker"));
            top.Add(Ui.Spacer());
            top.Add(Ui.Pill("Priority", "pill-priority"));
            assigned.Add(top);

            var route = Ui.Row("delivery-route", "wrap");
            route.Add(RouteStop("PICK UP", "Lekki Phase 1", "Cold room \u00B7 bay 2"));
            route.Add(Ui.Text(">", "route-arrow"));
            route.Add(RouteStop("HAND OFF", "Customer pickup", "Tola Adeyemi \u00B7 4 items"));
            assigned.Add(route);

            var details = Ui.Row("queue-body", "wrap");
            details.Add(Ui.Field("Order", "CR-8F2K", "queue-field"));
            details.Add(Ui.Field("Status", "AssignedToRider", "queue-field"));
            details.Add(Ui.Field("Urgency", "Priority", "queue-field"));
            assigned.Add(details);

            var actions = Ui.Row("queue-actions");
            actions.Add(Ui.Primary("Mark in transit", () => Shell.ShowToast("Rider transitions arrive in step 8.")));
            actions.Add(Ui.Secondary("Arrived at pickup", () => Shell.ShowToast("Rider transitions arrive in step 8.")));
            assigned.Add(actions);
            assigned.Add(Ui.Text("Mark Arrived at pickup when you reach the station. The customer then confirms receipt.", "fine-print"));
            main.Add(assigned);

            var timeline = Ui.Col("card");
            timeline.Add(Ui.Text("RUN TIMELINE", "card-kicker"));
            timeline.Add(Ui.Text("Three operator-controlled hand-offs", "card-title"));
            timeline.Add(TimelineRow("AssignedToRider", "You have the order.", true));
            timeline.Add(TimelineRow("InTransit", "On the way to the pickup station.", false));
            timeline.Add(TimelineRow("ArrivedAtPickup", "Waiting for the customer's scan.", false));
            main.Add(timeline);

            layout.Add(main);

            var aside = Ui.Col("two-col-aside");
            var barcode = Ui.Col("card", "barcode-card");
            barcode.Add(Ui.Text("ORDER BARCODE", "card-kicker"));
            barcode.Add(Ui.Text("Show this at pickup", "card-title"));
            barcode.Add(FakeBarcode());
            barcode.Add(Ui.Text("CR-8F2K", "barcode-code"));
            barcode.Add(Ui.Text("CR|o_8f2k|CR-8F2K", "barcode-payload"));
            barcode.Add(Ui.Text("The customer scans this with their phone camera, or types the short code.", "fine-print"));
            aside.Add(barcode);

            var note = Ui.Col("card", "aside-note");
            note.Add(Ui.Text("NO MAPS", "card-kicker"));
            note.Add(Ui.Text("Station-led pickups", "card-title"));
            note.Add(Ui.Text("This MVP does not include maps. Riders work from the pickup station and the order code.", "card-copy"));
            aside.Add(note);

            layout.Add(aside);
            root.Add(layout);
        }

        private static VisualElement RouteStop(string label, string title, string meta)
        {
            var col = Ui.Col("route-stop");
            col.Add(Ui.Text(label, "route-label"));
            col.Add(Ui.Text(title, "route-title"));
            col.Add(Ui.Text(meta, "route-meta"));
            return col;
        }

        private static VisualElement TimelineRow(string status, string copy, bool active)
        {
            var row = Ui.Row("timeline-row");
            row.Add(Ui.Dot(active ? "timeline-dot-active" : "timeline-dot"));
            var col = Ui.Col("timeline-copy");
            col.Add(Ui.Text(status, active ? "timeline-status-active" : "timeline-status"));
            col.Add(Ui.Text(copy, "timeline-desc"));
            row.Add(col);
            return row;
        }

        private static VisualElement FakeBarcode()
        {
            var frame = Ui.Col("barcode-frame");
            var bars = Ui.Row("barcode-bars");
            int[] widths = { 3, 1, 2, 4, 1, 3, 2, 1, 4, 2, 1, 3, 1, 4, 2, 3, 1, 2, 4, 1, 3, 2, 1, 4, 2, 3, 1, 1, 4, 2, 3, 1, 2, 4, 1, 3, 2, 1, 4, 2 };
            for (int i = 0; i < widths.Length; i++)
            {
                var bar = Ui.El("barcode-bar");
                bar.style.width = widths[i];
                if (i != widths.Length - 1)
                {
                    bar.style.marginRight = widths[i] == 1 ? 2 : 3;
                }

                bars.Add(bar);
            }

            frame.Add(bars);
            return frame;
        }
    }
}
