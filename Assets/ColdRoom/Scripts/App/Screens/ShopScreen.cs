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
    public sealed class ShopScreen : ScreenView
    {
        public override string Route => "shop";
        public override string NavLabel => "Shop";
        public override string Badge => "SH";
        public override string Eyebrow => "Customer \u00B7 Lagos";
        public override string Title => "Shop";
        public override string Subtitle => "Order chilled protein and produce, then collect it from the station nearest you.";

        private readonly List<Product> _products = new();
        private readonly List<Product> _allProducts = new();
        private readonly List<PickupStation> _stations = new();
        private readonly List<(ProductCategory? Category, VisualElement Tile, Label Count)> _categoryTiles = new();

        private VisualElement _catalogHost;
        private Label _catalogNote;
        private Label _productStat;
        private Label _stationStat;
        private Label _coldStationValue;
        private Label _coldPickupValue;
        private ProductCategory? _filter;
        private bool _loading;
        private bool _loaded;
        private int _requestId;

        protected override void Build(VisualElement root)
        {
            root.Add(PageHead());
            root.Add(BuildHero());
            root.Add(BuildCategories());
            root.Add(BuildCatalog());
            root.Add(BuildPlan());
        }

        public override void OnShow()
        {
            if (!_loaded && !_loading)
            {
                _ = LoadAsync();
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
                "CamaanStores keeps meat, fish, poultry and produce at temperature while you get on with your day. " +
                "Pick a station, choose your urgency, and collect when it suits you.",
                "hero-sub"));

            var actions = Ui.Row("hero-actions", "wrap");
            actions.Add(Ui.Primary("Browse catalog", BrowseCatalog));
            actions.Add(Ui.Secondary("Request storage", () => Shell.Navigate("storage")));
            copy.Add(actions);

            var meta = Ui.Row("hero-meta", "wrap");
            var products = Stat("\u2014", "products in the cold room");
            _productStat = products.Q<Label>(className: "stat-value");
            meta.Add(products);
            var stations = Stat("\u2014", "pickup stations");
            _stationStat = stations.Q<Label>(className: "stat-value");
            meta.Add(stations);
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
            top.Add(Ui.Pill("Lagos", "pill-outline"));
            card.Add(top);

            var ring = Ui.Col("temp-ring");
            var inner = Ui.Col("temp-ring-inner");
            inner.Add(Ui.Text("3.1\u00B0C", "temp-value"));
            inner.Add(Ui.Text("holding temp", "temp-label"));
            ring.Add(inner);
            card.Add(ring);

            var rows = Ui.Col("cold-rows");
            var stationRow = InfoRow("Stations", "Checking\u2026");
            _coldStationValue = stationRow.Q<Label>(className: "info-value");
            rows.Add(stationRow);
            var pickupRow = InfoRow("Default pickup", "Checking\u2026");
            _coldPickupValue = pickupRow.Q<Label>(className: "info-value");
            rows.Add(pickupRow);
            rows.Add(InfoRow("Storage bays", "6 available"));
            card.Add(rows);

            return card;
        }

        private VisualElement BuildCategories()
        {
            var section = Ui.Col("section");
            _catalogNote = Ui.Text("Loading catalog\u2026", "section-note");
            section.Add(Ui.SectionHead("Shop by category", "What we keep cold", _catalogNote));

            var grid = Ui.Row("grid", "grid-4");
            grid.Add(CategoryTile(null, "00", "All", "Every cold item", "ALL"));
            grid.Add(CategoryTile(ProductCategory.Meat, "01", "Meat", "Beef, goat and cuts", "MEAT"));
            grid.Add(CategoryTile(ProductCategory.Fish, "02", "Fish", "Catfish and more", "FISH"));
            grid.Add(CategoryTile(ProductCategory.Poultry, "03", "Poultry", "Chicken and turkey", "POULTRY"));
            grid.Add(CategoryTile(ProductCategory.Vegetables, "04", "Vegetables", "Leafy greens and peppers", "PRODUCE"));
            grid.Add(CategoryTile(ProductCategory.Other, "05", "Other", "Snails and specials", "OTHER"));
            section.Add(grid);
            return section;
        }

        private VisualElement CategoryTile(ProductCategory? category, string number, string title, string copy, string actionLabel)
        {
            var tile = Ui.Col("category-tile");
            var top = Ui.Row("category-top");
            top.Add(Ui.Text(number, "category-number"));
            top.Add(Ui.Spacer());
            top.Add(Ui.Text(actionLabel, "category-action"));
            tile.Add(top);
            tile.Add(Ui.Text(title, "category-name"));
            var count = Ui.Text(copy, "category-copy");
            tile.Add(count);
            tile.EnableInClassList("active", category == null);
            Ui.Clickable(tile, () => SetFilter(category));

            _categoryTiles.Add((category, tile, count));
            return tile;
        }

        private VisualElement BuildCatalog()
        {
            var section = Ui.Col("section");
            section.Add(Ui.SectionHead(
                "Fresh in the cold room",
                "Live catalog",
                Ui.Pill("Mock API", "pill-soft")));

            _catalogHost = Ui.Col("catalog-host");
            _catalogHost.Add(LoadingState("Loading the cold room\u2026"));
            section.Add(_catalogHost);
            return section;
        }

        private VisualElement BuildPlan()
        {
            var section = Ui.Col("section");
            section.Add(Ui.SectionHead("Prototype status", "Build plan",
                Ui.Pill("Step 2 \u00B7 Auth & profile", "pill-accent")));

            var card = Ui.Col("card", "plan-card");
            card.Add(Ui.Text(
                "Accounts and the live catalog are wired to the mock API. Cart and checkout are next.",
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
                string state = i <= 1 ? "Live" : i == 2 ? "Next" : "Queued";
                row.Add(Ui.Pill(state, i <= 1 ? "pill-accent" : "pill-soft"));
                if (i == 1)
                {
                    row.AddToClassList("plan-row-current");
                }

                grid.Add(row);
            }

            card.Add(grid);
            section.Add(card);
            return section;
        }

        private void BrowseCatalog()
        {
            if (_filter == null)
            {
                _ = LoadAsync();
                return;
            }

            SetFilter(null);
        }

        private void SetFilter(ProductCategory? category)
        {
            if (_filter == category)
            {
                return;
            }

            _filter = category;
            foreach (var entry in _categoryTiles)
            {
                entry.Tile.EnableInClassList("active", entry.Category == _filter);
            }

            _ = LoadAsync();
        }

        private async Task LoadAsync()
        {
            int request = ++_requestId;
            _loading = true;
            _catalogNote.text = "Loading catalog\u2026";
            _catalogHost.Clear();
            _catalogHost.Add(LoadingState("Loading the cold room\u2026"));

            try
            {
                Task<IReadOnlyList<Product>> filteredTask = Shell.Api.GetProducts(_filter);
                Task<IReadOnlyList<Product>> allTask = Shell.Api.GetProducts();
                Task<IReadOnlyList<PickupStation>> stationsTask = Shell.Api.GetStations();
                await Task.WhenAll(filteredTask, allTask, stationsTask);
                if (request != _requestId)
                {
                    return;
                }

                _products.Clear();
                _products.AddRange(filteredTask.Result);
                _allProducts.Clear();
                _allProducts.AddRange(allTask.Result);
                _stations.Clear();
                _stations.AddRange(stationsTask.Result);

                _loaded = true;
                UpdateHero();
                UpdateCategoryCounts();
                RenderCatalog();
            }
            catch (ApiException exception)
            {
                if (request == _requestId)
                {
                    ShowCatalogError(exception.Message);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[ColdRoom] Catalog load failed: {exception.Message}");
                if (request == _requestId)
                {
                    ShowCatalogError("The catalog could not be loaded.");
                }
            }
            finally
            {
                if (request == _requestId)
                {
                    _loading = false;
                }
            }
        }

        private void ShowCatalogError(string message)
        {
            _catalogNote.text = "Could not load catalog";
            _catalogHost.Clear();
            _catalogHost.Add(ErrorState("Catalog unavailable", message, () => _ = LoadAsync()));
        }

        private void UpdateHero()
        {
            _productStat.text = _allProducts.Count.ToString();
            _stationStat.text = _stations.Count.ToString();
            _coldStationValue.text = $"{_stations.Count} of {_stations.Count} online";

            PickupStation station = _stations.FirstOrDefault(s => s.Id == Shell.User.DefaultStationId)
                                    ?? _stations.FirstOrDefault();
            _coldPickupValue.text = station != null ? station.Name : "Choose a station";
        }

        private void UpdateCategoryCounts()
        {
            foreach (var entry in _categoryTiles)
            {
                int count = entry.Category.HasValue
                    ? _allProducts.Count(p => p.Category == entry.Category.Value)
                    : _allProducts.Count;
                entry.Count.text = count == 1 ? "1 product" : $"{count} products";
                entry.Tile.EnableInClassList("active", entry.Category == _filter);
            }
        }

        private void RenderCatalog()
        {
            _catalogHost.Clear();
            _catalogNote.text = $"{_products.Count} of {_allProducts.Count} products \u00B7 {LagosTime.Clock}";

            if (_products.Count == 0)
            {
                _catalogHost.Add(EmptyState(
                    "SH",
                    "Nothing in this category",
                    "Try another category or refresh the catalog from the mock API.",
                    ("Show all products", () => SetFilter(null))));
                return;
            }

            var grid = Ui.Row("grid", "grid-3", "product-grid");
            foreach (Product product in _products)
            {
                grid.Add(ProductCard(product));
            }

            _catalogHost.Add(grid);
        }

        private static VisualElement ProductCard(Product product)
        {
            var card = Ui.Col("card", "product-card");

            var top = Ui.Row("product-top");
            top.Add(Ui.Text(product.Category.ToString().ToUpperInvariant(), "product-category"));
            top.Add(Ui.Spacer());
            if (product.IsPerishable)
            {
                top.Add(Ui.Pill("Perishable", "pill-warn"));
            }

            card.Add(top);
            card.Add(Ui.Text(product.Name, "product-name"));

            var priceRow = Ui.Row("product-price-row");
            priceRow.Add(Ui.Text(Money.Format(product.PriceKobo), "product-price"));
            priceRow.Add(Ui.Text("/ " + product.Unit, "product-unit"));
            card.Add(priceRow);

            var meta = Ui.Row("product-meta", "wrap");
            meta.Add(Ui.Text($"{product.StockQty} {product.Unit} in stock", "product-stock"));
            meta.Add(Ui.Pill(product.RecommendedUrgency.ToString(), UrgencyClass(product.RecommendedUrgency)));
            card.Add(meta);

            card.Add(Ui.Text(
                product.IsPerishable ? "Keep cold \u00B7 best within 48 hours" : "Chilled \u00B7 check at pickup",
                "product-foot"));

            return card;
        }

        private static string UrgencyClass(OrderUrgency urgency)
        {
            return urgency switch
            {
                OrderUrgency.Urgent => "pill-urgent",
                OrderUrgency.Priority => "pill-priority",
                _ => "pill-standard"
            };
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
