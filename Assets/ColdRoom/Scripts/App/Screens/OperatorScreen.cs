using UnityEngine.UIElements;

namespace ColdRoom.App.Screens
{
    public sealed class OperatorScreen : ScreenView
    {
        public override string Route => "operator";
        public override string NavLabel => "Queue";
        public override string Badge => "QU";
        public override string Eyebrow => "Operator \u00B7 Lagos";
        public override string Title => "Order queue";
        public override string Subtitle => "Urgent first, then Priority, then Standard. Oldest first within each level.";

        protected override void Build(VisualElement root)
        {
            root.Add(PageHead());

            var metrics = Ui.Row("grid", "grid-3", "metric-grid");
            metrics.Add(Metric("Urgent", "1", "oldest 12 min", "metric-urgent"));
            metrics.Add(Metric("Priority", "2", "oldest 26 min", "metric-priority"));
            metrics.Add(Metric("Standard", "3", "oldest 54 min", "metric-standard"));
            root.Add(metrics);

            var layout = Ui.Row("two-col");
            var main = Ui.Col("two-col-main");

            var queueHead = Ui.Row("queue-head");
            queueHead.Add(Ui.Text("READY TO PROCESS", "card-kicker"));
            queueHead.Add(Ui.Spacer());
            queueHead.Add(Ui.Pill("Preview queue", "pill-soft"));
            main.Add(queueHead);

            main.Add(QueueRow("CR-8F2K", "Urgent", "12 min", "Tola Adeyemi", "Lekki Phase 1", "4 items", "urgent"));
            main.Add(QueueRow("CR-2M7Q", "Priority", "26 min", "Chidi Okafor", "Yaba", "2 items", "priority"));
            main.Add(QueueRow("CR-9T4B", "Standard", "54 min", "Amaka Obi", "Ikeja", "6 items", "standard"));

            var footer = Ui.Row("queue-footer");
            footer.Add(Ui.Text("Live queue and status transitions arrive in step 7.", "fine-print"));
            footer.Add(Ui.Spacer());
            footer.Add(Ui.Ghost("Refresh queue", () => Shell.ShowToast("The live queue arrives in step 7.")));
            main.Add(footer);

            layout.Add(main);

            var aside = Ui.Col("two-col-aside");
            var rules = Ui.Col("card");
            rules.Add(Ui.Text("QUEUE ORDER", "card-kicker"));
            rules.Add(Ui.Text("How work is picked", "card-title"));
            var r1 = Ui.Row("rule-row");
            r1.Add(Ui.Text("01", "rule-number"));
            r1.Add(Ui.Text("Urgent", "rule-name"));
            rules.Add(r1);
            var r2 = Ui.Row("rule-row");
            r2.Add(Ui.Text("02", "rule-number"));
            r2.Add(Ui.Text("Priority", "rule-name"));
            rules.Add(r2);
            var r3 = Ui.Row("rule-row");
            r3.Add(Ui.Text("03", "rule-number"));
            r3.Add(Ui.Text("Standard", "rule-name"));
            rules.Add(r3);
            rules.Add(Ui.Divider());
            rules.Add(Ui.Text("Within each level, orders are shown oldest first. The queue is enforced by the data layer, not just sort in the UI.", "card-copy"));
            aside.Add(rules);

            var flow = Ui.Col("card");
            flow.Add(Ui.Text("OPERATOR ACTIONS", "card-kicker"));
            flow.Add(Ui.Text("Move orders forward", "card-title"));
            flow.Add(FlowLine("Confirm", "Placed \u2192 Confirmed"));
            flow.Add(FlowLine("Pack", "Confirmed \u2192 Packed"));
            flow.Add(FlowLine("Assign", "Packed \u2192 AssignedToRider"));
            flow.Add(Ui.Text("Storage requests are approved from the Storage screen.", "fine-print"));
            aside.Add(flow);

            layout.Add(aside);
            root.Add(layout);
        }

        private VisualElement Metric(string label, string value, string meta, string className)
        {
            var card = Ui.Col("card", "metric-card", className);
            card.Add(Ui.Text(label.ToUpperInvariant(), "metric-label"));
            card.Add(Ui.Text(value, "metric-value"));
            card.Add(Ui.Text(meta, "metric-meta"));
            return card;
        }

        private VisualElement QueueRow(string code, string priority, string age, string customer, string station, string items, string priorityClass)
        {
            var card = Ui.Col("card", "queue-row");
            var top = Ui.Row("queue-top");
            top.Add(Ui.Text(code, "queue-code"));
            top.Add(Ui.Pill(priority, "pill-" + priorityClass));
            top.Add(Ui.Spacer());
            top.Add(Ui.Text(age + " old", "queue-age"));
            card.Add(top);

            var body = Ui.Row("queue-body", "wrap");
            body.Add(Ui.Field("Customer", customer, "queue-field"));
            body.Add(Ui.Field("Pickup", station, "queue-field"));
            body.Add(Ui.Field("Items", items, "queue-field"));
            card.Add(body);

            var actions = Ui.Row("queue-actions");
            actions.Add(Ui.Secondary("Open order", () => Shell.ShowToast("Order actions arrive in step 7.")));
            actions.Add(Ui.Ghost("Assign rider", () => Shell.ShowToast("Rider assignment arrives in step 7.")));
            card.Add(actions);

            return card;
        }

        private static VisualElement FlowLine(string action, string transition)
        {
            var row = Ui.Row("flow-line");
            row.Add(Ui.Text(action, "flow-line-action"));
            row.Add(Ui.Spacer());
            row.Add(Ui.Text(transition, "flow-line-status"));
            return row;
        }

        private static VisualElement RuleRow(string number, string name)
        {
            var row = Ui.Row("rule-row");
            row.Add(Ui.Text(number, "rule-number"));
            row.Add(Ui.Text(name, "rule-name"));
            return row;
        }
    }
}
