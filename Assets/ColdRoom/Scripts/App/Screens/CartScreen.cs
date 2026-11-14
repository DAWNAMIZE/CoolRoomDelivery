using UnityEngine.UIElements;

namespace ColdRoom.App.Screens
{
    public sealed class CartScreen : ScreenView
    {
        public override string Route => "cart";
        public override string NavLabel => "Cart";
        public override string Badge => "CT";
        public override string Eyebrow => "Customer";
        public override string Title => "Cart";
        public override string Subtitle => "A single cart can mix meat, fish, poultry and produce.";

        protected override void Build(VisualElement root)
        {
            root.Add(PageHead());

            var layout = Ui.Row("two-col");
            var main = Ui.Col("two-col-main");

            main.Add(EmptyState(
                "CT",
                "Your cart is empty",
                "Add chilled items from the catalog and they will appear here. Prices are stored in kobo and shown in NGN at checkout.",
                ("Browse the catalog", () => Shell.Navigate("shop"))));

            var info = Ui.Col("card");
            info.Add(Ui.Text("Checkout comes next", "card-title"));
            info.Add(Ui.Text("The cart will carry quantities, line totals and a station preference into checkout.", "card-copy"));
            var steps = Ui.Col("mini-flow");
            steps.Add(FlowStep("01", "Pick items", "Mix categories in one cart."));
            steps.Add(FlowStep("02", "Choose urgency", "Standard, Priority or Urgent."));
            steps.Add(FlowStep("03", "Choose a station", "Lekki, Yaba, Ikeja or Surulere."));
            info.Add(steps);
            main.Add(info);

            layout.Add(main);

            var aside = Ui.Col("two-col-aside");
            var summary = Ui.Col("card", "summary-card");
            summary.Add(Ui.Text("ORDER SUMMARY", "summary-label"));
            summary.Add(SummaryRow("Items", "0"));
            summary.Add(SummaryRow("Subtotal", "\u20A60"));
            summary.Add(Ui.Divider());
            var total = Ui.Row("summary-total");
            total.Add(Ui.Text("Total", "summary-total-label"));
            total.Add(Ui.Spacer());
            total.Add(Ui.Text("\u20A60", "summary-total-value"));
            summary.Add(total);
            summary.Add(Ui.Primary("Checkout", () => Shell.ShowToast("Checkout arrives in step 5.")));
            aside.Add(summary);

            var note = Ui.Col("card", "aside-note");
            note.Add(Ui.Text("URGENCY", "card-kicker"));
            note.Add(Ui.Text("Per order, not per item", "card-title"));
            note.Add(Ui.Text("You will choose one urgency for the whole order at checkout.", "card-copy"));
            aside.Add(note);

            layout.Add(aside);
            root.Add(layout);
        }

        private static VisualElement FlowStep(string number, string title, string copy)
        {
            var row = Ui.Row("flow-step");
            row.Add(Ui.Text(number, "flow-number"));
            var col = Ui.Col("flow-copy");
            col.Add(Ui.Text(title, "flow-title"));
            col.Add(Ui.Text(copy, "flow-copy-text"));
            row.Add(col);
            return row;
        }

        private static VisualElement SummaryRow(string label, string value)
        {
            var row = Ui.Row("summary-row");
            row.Add(Ui.Text(label, "summary-key"));
            row.Add(Ui.Spacer());
            row.Add(Ui.Text(value, "summary-value"));
            return row;
        }
    }
}
