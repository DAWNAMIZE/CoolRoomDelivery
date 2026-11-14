using System;
using UnityEngine.UIElements;

namespace ColdRoom.App.Screens
{
    public sealed class OrdersScreen : ScreenView
    {
        public override string Route => "orders";
        public override string NavLabel => "Orders";
        public override string Badge => "OR";
        public override string Eyebrow => "Customer";
        public override string Title => "Orders";
        public override string Subtitle => "Every order moves through one clear pipeline, from placement to receipt.";

        private static readonly string[] Statuses =
        {
            "Placed",
            "Confirmed",
            "Packed",
            "AssignedToRider",
            "InTransit",
            "ArrivedAtPickup",
            "Received"
        };

        protected override void Build(VisualElement root)
        {
            root.Add(PageHead());

            root.Add(EmptyState(
                "OR",
                "No orders yet",
                "Place an order and track its status here. When the rider arrives at your pickup station, confirm receipt with a barcode scan or short code.",
                ("Start an order", () => Shell.Navigate("shop"))));

            var pipeline = Ui.Col("section");
            pipeline.Add(Ui.SectionHead("The order pipeline", "Seven checkpoints",
                Ui.Text("UTC stored \u00B7 Lagos displayed", "section-note")));

            var card = Ui.Col("card", "pipeline-card");
            var flow = Ui.Row("pipeline", "wrap");
            for (int i = 0; i < Statuses.Length; i++)
            {
                var step = Ui.Col("pipeline-step");
                step.Add(Ui.Text((i + 1).ToString("00"), "pipeline-number"));
                step.Add(Ui.Text(Statuses[i], "pipeline-name"));
                if (i == 1 || i == 2)
                {
                    step.Add(Ui.Pill("cancel allowed", "pill-warn"));
                }
                else if (i == 0)
                {
                    step.Add(Ui.Pill("cancel allowed", "pill-warn"));
                }
                else
                {
                    step.Add(Ui.Text(" ", "pipeline-spacer"));
                }

                flow.Add(step);
                if (i < Statuses.Length - 1)
                {
                    flow.Add(Ui.Text(">", "pipeline-arrow"));
                }
            }

            card.Add(flow);
            pipeline.Add(card);

            var rules = Ui.Row("grid", "grid-2");
            rules.Add(RuleCard(
                "CANCELLATION",
                "Only before packing",
                "An order can be cancelled from Placed or Confirmed. Once it reaches Packed, the cold room has started handling it."));
            rules.Add(RuleCard(
                "RECEIPT",
                "Two ways to confirm",
                "Scan the order barcode with the phone camera, or type the six-character short code shown on the rider's screen."));
            pipeline.Add(rules);

            root.Add(pipeline);
        }

        private static VisualElement RuleCard(string kicker, string title, string copy)
        {
            var card = Ui.Col("card", "rule-card");
            card.Add(Ui.Text(kicker, "card-kicker"));
            card.Add(Ui.Text(title, "card-title"));
            card.Add(Ui.Text(copy, "card-copy"));
            return card;
        }
    }
}
