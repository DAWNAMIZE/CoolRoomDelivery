using System;
using System.Collections.Generic;
using ColdRoom.Core;
using UnityEngine.UIElements;

namespace ColdRoom.App.Screens
{
    public sealed class ShopScreen : ScreenView
    {
        public override string Route => "shop";
        public override string NavLabel => "Shop";
        public override string Badge => "SH";
        public override string Eyebrow => "Customer \u00B7 Lagos";
        public override string Title => "Shop";
        public override string Subtitle => "Order chilled protein and produce, then collect it from the station nearest you.";

        private readonly List<(UserRole Role, VisualElement Card)> _roleCards = new();

        protected override void Build(VisualElement root)
        {
            root.Add(PageHead());
            root.Add(BuildHero());
            root.Add(BuildCategories());
            root.Add(BuildRoles());
            root.Add(BuildPlan());
        }

        public override void OnShow()
        {
            foreach (var entry in _roleCards)
            {
                entry.Card.EnableInClassList("active", entry.Role == Shell.Role);
            }
        }

        private VisualElement BuildHero()
        {
            var hero = Ui.Row("hero", "hero-shop");

            var copy = Ui.Col("hero-copy");
            var kicker = Ui.Row("hero-kicker");
            kicker.Add(Ui.Dot("pulse-dot"));
            kicker.Add(Ui.Text("FRESH FROM THE COLD ROOM", "hero-kicker-text"));
            copy.Add(kicker);

            copy.Add(Ui.Text("Cold chain,\nwithout the chaos.", "hero-title"));
            copy.Add(Ui.Text(
                "ColdRoom keeps meat, fish, poultry and produce at temperature while you get on with your day. " +
                "Pick a station, choose your urgency, and collect when it suits you.",
                "hero-sub"));

            var actions = Ui.Row("hero-actions", "wrap");
            actions.Add(Ui.Primary("Browse catalog", () => Shell.ShowToast("The catalog lands in step 3.")));
            actions.Add(Ui.Secondary("Request storage", () => Shell.Navigate("storage")));
            copy.Add(actions);

            var meta = Ui.Row("hero-meta", "wrap");
            meta.Add(Stat("4", "pickup stations"));
            meta.Add(Stat("06:00\u201322:00", "open daily"));
            meta.Add(Stat("2\u20134\u00B0C", "holding range"));
            copy.Add(meta);

            hero.Add(copy);
            hero.Add(BuildColdRoomCard());
            return hero;
        }

        private VisualElement BuildColdRoomCard()
        {
            var card = Ui.Col("cold-card");
            var top = Ui.Row("cold-card-top");
            top.Add(Ui.Dot("pulse-dot"));
            top.Add(Ui.Text("COLD ROOM ONLINE", "cold-status"));
            top.Add(Ui.Spacer());
            top.Add(Ui.Pill("Lekki", "pill-outline"));
            card.Add(top);

            var ring = Ui.Col("temp-ring");
            var inner = Ui.Col("temp-ring-inner");
            inner.Add(Ui.Text("3.1\u00B0C", "temp-value"));
            inner.Add(Ui.Text("holding temp", "temp-label"));
            ring.Add(inner);
            card.Add(ring);

            var rows = Ui.Col("cold-rows");
            rows.Add(InfoRow("Stations", "4 of 4 online"));
            rows.Add(InfoRow("Next pickup", "Lekki Phase 1 \u00B7 18:30"));
            rows.Add(InfoRow("Storage bays", "6 available"));
            card.Add(rows);

            return card;
        }

        private VisualElement BuildCategories()
        {
            var section = Ui.Col("section");
            section.Add(Ui.SectionHead("Shop by category", "What we keep cold",
                Ui.Text("10 products at launch", "section-note")));

            var grid = Ui.Row("grid", "grid-4");
            grid.Add(CategoryTile("01", "Meat", "Beef, goat, offal and cuts", "MEAT"));
            grid.Add(CategoryTile("02", "Fish", "Croaker, mackerel, catfish", "FISH"));
            grid.Add(CategoryTile("03", "Poultry", "Chicken, turkey and eggs", "POULTRY"));
            grid.Add(CategoryTile("04", "Vegetables", "Leafy greens and peppers", "PRODUCE"));
            section.Add(grid);
            return section;
        }

        private VisualElement CategoryTile(string number, string title, string copy, string actionLabel)
        {
            var tile = Ui.Col("category-tile");
            var top = Ui.Row("category-top");
            top.Add(Ui.Text(number, "category-number"));
            top.Add(Ui.Spacer());
            top.Add(Ui.Text(actionLabel, "category-action"));
            tile.Add(top);
            tile.Add(Ui.Text(title, "category-name"));
            tile.Add(Ui.Text(copy, "category-copy"));
            Ui.Clickable(tile, () => Shell.ShowToast($"{title} products arrive in step 3."));
            return tile;
        }

        private VisualElement BuildRoles()
        {
            var section = Ui.Col("section");
            section.Add(Ui.SectionHead("One build, three roles", "Preview the shell",
                Ui.Text("Switch role in the rail", "section-note")));

            var grid = Ui.Row("grid", "grid-3", "role-grid");
            grid.Add(RoleCard(
                UserRole.Customer,
                "Customer",
                "Order, store, confirm.",
                "Browse the cold room, build a cart, choose urgency and confirm receipt with a scan.",
                new[] { "Cart", "Urgency", "Barcode" }));
            grid.Add(RoleCard(
                UserRole.Operator,
                "Operator",
                "Queue, dispatch, approve.",
                "Work the priority queue, move orders through statuses, assign riders and clear storage requests.",
                new[] { "Queue", "Riders", "Storage" }));
            grid.Add(RoleCard(
                UserRole.Rider,
                "Rider",
                "Run the pickup.",
                "See assigned runs, mark transit and arrival, and present the order barcode at the station.",
                new[] { "Assigned runs", "Transit", "Barcode" }));
            section.Add(grid);
            return section;
        }

        private VisualElement RoleCard(UserRole role, string title, string tagline, string copy, string[] tags)
        {
            var card = Ui.Col("card", "role-card");
            var top = Ui.Row("role-top");
            top.Add(Ui.Monogram(role == UserRole.Customer ? "CU" : role == UserRole.Operator ? "OP" : "RI", "role-mark"));
            top.Add(Ui.Spacer());
            top.Add(Ui.Pill("Preview", "pill-soft"));
            card.Add(top);
            card.Add(Ui.Text(title, "role-title"));
            card.Add(Ui.Text(tagline, "role-tagline"));
            card.Add(Ui.Text(copy, "role-copy"));
            card.Add(Ui.TagRow(tags));
            var foot = Ui.Row("role-foot");
            foot.Add(Ui.Text("Open this view", "role-foot-text"));
            foot.Add(Ui.Spacer());
            foot.Add(Ui.Text(">", "role-arrow"));
            card.Add(foot);

            _roleCards.Add((role, card));
            Ui.Clickable(card, () => Shell.SetRole(role));
            return card;
        }

        private VisualElement BuildPlan()
        {
            var section = Ui.Col("section");
            section.Add(Ui.SectionHead("Prototype status", "Build plan",
                Ui.Pill("Step 1 \u00B7 App shell", "pill-accent")));

            var card = Ui.Col("card", "plan-card");
            card.Add(Ui.Text(
                "The shell is live. Screens below are previews until their step is wired to the data layer.",
                "plan-intro"));

            string[] steps =
            {
                "Project setup & app shell",
                "Auth & profile",
                "Catalog",
                "Cart",
                "Checkout & urgency",
                "Order history & tracking",
                "Operator queue",
                "Rider flow",
                "Barcode confirmation",
                "Storage requests",
                "WebGL build"
            };

            var grid = Ui.Row("plan-grid");
            for (int i = 0; i < steps.Length; i++)
            {
                var row = Ui.Row("plan-row");
                row.Add(Ui.Text((i + 1).ToString("00"), "plan-index"));
                row.Add(Ui.Text(steps[i], "plan-name"));
                row.Add(Ui.Spacer());
                string state = i == 0 ? "Live" : i == 1 ? "Next" : "Queued";
                row.Add(Ui.Pill(state, i == 0 ? "pill-accent" : "pill-soft"));
                if (i == 0)
                {
                    row.AddToClassList("plan-row-current");
                }

                grid.Add(row);
            }

            card.Add(grid);
            section.Add(card);
            return section;
        }

        private static VisualElement Stat(string value, string label)
        {
            var stat = Ui.Col("stat");
            stat.Add(Ui.Text(value, "stat-value"));
            stat.Add(Ui.Text(label, "stat-label"));
            return stat;
        }

        private static VisualElement InfoRow(string label, string value)
        {
            var row = Ui.Row("info-row");
            row.Add(Ui.Text(label, "info-label"));
            row.Add(Ui.Spacer());
            row.Add(Ui.Text(value, "info-value"));
            return row;
        }
    }
}
