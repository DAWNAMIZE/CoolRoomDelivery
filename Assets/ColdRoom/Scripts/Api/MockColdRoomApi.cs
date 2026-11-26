using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ColdRoom.Core;
using ColdRoom.Core.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using UnityEngine;

namespace ColdRoom.Api
{
    /// <summary>
    /// Local API used by the app until the hosted backend exists. All state lives in one
    /// JSON file under Application.persistentDataPath, so reloads and restarts keep data.
    /// Passwords are stored as salted PBKDF2 hashes; plaintext never touches disk.
    /// </summary>
    public sealed class MockColdRoomApi : IColdRoomApi
    {
        private const int LatencyMs = 90;
        private const int HashIterations = 60000;
        private const string DatabaseFileName = "coldroom-db.json";

        private static readonly Regex EmailPattern = new(
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex PhonePattern = new(
            @"^\+?[0-9][0-9\s\-]{8,18}$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly JsonSerializerSettings JsonSettings = new()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Converters = { new StringEnumConverter() },
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
            DateParseHandling = DateParseHandling.None
        };

        private readonly string _path;
        private readonly SemaphoreSlim _gate = new(1, 1);
        private Database _db;

        public MockColdRoomApi()
        {
            _path = Path.Combine(Application.persistentDataPath, DatabaseFileName);
        }

        public string DatabasePath => _path;

        public Task<AuthResult> Register(string email, string password, string fullName, string phone)
        {
            return RunAsync(() => RegisterInternal(email, password, fullName, phone));
        }

        public Task<AuthResult> Login(string email, string password)
        {
            return RunAsync(() => LoginInternal(email, password));
        }

        public Task Logout()
        {
            return RunAsync(() =>
            {
                string token = SessionStore.Token;
                if (!string.IsNullOrWhiteSpace(token) && _db.Sessions.RemoveAll(s => s.Token == token) > 0)
                {
                    Save();
                }

                SessionStore.Clear();
                return true;
            });
        }

        public Task<User> GetMe()
        {
            return RunAsync(() => RequireUser());
        }

        public Task<User> UpdateMe(string fullName, string phone, string defaultStationId)
        {
            return RunAsync(() => UpdateMeInternal(fullName, phone, defaultStationId));
        }

        public Task<IReadOnlyList<Product>> GetProducts(ProductCategory? category = null)
        {
            return RunAsync<IReadOnlyList<Product>>(() =>
            {
                IEnumerable<Product> query = _db.Products;
                if (category.HasValue)
                {
                    query = query.Where(p => p.Category == category.Value);
                }

                return query.ToList();
            });
        }

        public Task<IReadOnlyList<PickupStation>> GetStations()
        {
            return RunAsync<IReadOnlyList<PickupStation>>(() => _db.Stations.ToList());
        }

        public Task<IReadOnlyList<CartLine>> GetCart()
        {
            return RunAsync<IReadOnlyList<CartLine>>(() =>
            {
                User user = RequireCustomer();
                return CloneCart(GetCartFor(user.Id));
            });
        }

        public Task<IReadOnlyList<CartLine>> UpdateCart(IReadOnlyList<CartLine> lines)
        {
            return RunAsync<IReadOnlyList<CartLine>>(() => UpdateCartInternal(lines));
        }

        public Task<Order> PlaceOrder(string stationId, OrderUrgency urgency)
        {
            return RunAsync(() => PlaceOrderInternal(stationId, urgency));
        }

        public Task<IReadOnlyList<Order>> GetOrders()
        {
            return RunAsync<IReadOnlyList<Order>>(() =>
            {
                User user = RequireUser();
                IEnumerable<Order> query = user.Role switch
                {
                    UserRole.Customer => _db.Orders.Where(o => o.CustomerId == user.Id),
                    UserRole.Operator => _db.Orders,
                    UserRole.Rider => _db.Orders.Where(o => o.RiderId == user.Id),
                    _ => Array.Empty<Order>()
                };

                return query.OrderByDescending(o => o.CreatedAt, StringComparer.Ordinal).ToList();
            });
        }

        public Task<Order> GetOrder(string orderId)
        {
            return RunAsync(() =>
            {
                User user = RequireUser();
                Order order = FindOrder(orderId);
                bool allowed = user.Role == UserRole.Operator
                               || (user.Role == UserRole.Customer && order.CustomerId == user.Id)
                               || (user.Role == UserRole.Rider && order.RiderId == user.Id);

                if (!allowed)
                {
                    throw Fail(ApiErrorCodes.Unauthorized, "You cannot view this order.");
                }

                return order;
            });
        }

        public Task<Order> TransitionOrder(string orderId, OrderStatus status, string riderId = null)
        {
            return RunAsync(() => TransitionOrderInternal(orderId, status, riderId));
        }

        public Task<Order> ConfirmReceipt(string orderId)
        {
            return RunAsync(() =>
            {
                User user = RequireUser();
                if (user.Role != UserRole.Customer)
                {
                    throw Fail(ApiErrorCodes.Unauthorized, "Only the customer can confirm receipt.");
                }

                Order order = FindOrder(orderId);
                if (order.CustomerId != user.Id)
                {
                    throw Fail(ApiErrorCodes.Unauthorized, "This order belongs to another customer.");
                }

                if (order.Status != OrderStatus.ArrivedAtPickup)
                {
                    throw Fail(ApiErrorCodes.IllegalTransition,
                        $"Order {order.PublicCode} cannot be received while it is {order.Status}.");
                }

                order.Status = OrderStatus.Received;
                order.UpdatedAt = NowIso();
                Save();
                return order;
            });
        }

        public Task<StorageRequest> CreateStorageRequest(string description, double estimatedKg)
        {
            return RunAsync(() =>
            {
                User user = RequireCustomer();
                string trimmed = (description ?? string.Empty).Trim();
                if (trimmed.Length < 3)
                {
                    throw Fail(ApiErrorCodes.ValidationError, "Describe the goods you want to store.");
                }

                if (estimatedKg <= 0 || estimatedKg > 1000)
                {
                    throw Fail(ApiErrorCodes.ValidationError, "Estimated weight must be between 1 and 1000 kg.");
                }

                var request = new StorageRequest
                {
                    Id = NewId("sr"),
                    CustomerId = user.Id,
                    Description = trimmed,
                    EstimatedKg = estimatedKg,
                    Status = StorageStatus.Requested,
                    CreatedAt = NowIso()
                };

                _db.StorageRequests.Add(request);
                Save();
                return request;
            });
        }

        public Task<IReadOnlyList<StorageRequest>> GetStorageRequests()
        {
            return RunAsync<IReadOnlyList<StorageRequest>>(() =>
            {
                User user = RequireUser();
                IEnumerable<StorageRequest> query = user.Role switch
                {
                    UserRole.Customer => _db.StorageRequests.Where(r => r.CustomerId == user.Id),
                    UserRole.Operator => _db.StorageRequests,
                    _ => Array.Empty<StorageRequest>()
                };

                return query.OrderByDescending(r => r.CreatedAt, StringComparer.Ordinal).ToList();
            });
        }

        public Task<StorageRequest> TransitionStorageRequest(string storageRequestId, StorageStatus status)
        {
            return RunAsync(() => TransitionStorageRequestInternal(storageRequestId, status));
        }

        public Task<IReadOnlyList<User>> GetRiders()
        {
            return RunAsync<IReadOnlyList<User>>(() =>
            {
                User user = RequireUser();
                if (user.Role != UserRole.Operator)
                {
                    throw Fail(ApiErrorCodes.Unauthorized, "Only operators can view the rider list.");
                }

                return _db.Users
                    .Where(u => u.Role == UserRole.Rider)
                    .OrderBy(u => u.FullName, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            });
        }

        private async Task<T> RunAsync<T>(Func<T> action)
        {
            await Task.Delay(LatencyMs);
            await _gate.WaitAsync();
            try
            {
                EnsureLoaded();
                return action();
            }
            finally
            {
                _gate.Release();
            }
        }

        private AuthResult RegisterInternal(string email, string password, string fullName, string phone)
        {
            string normalizedEmail = NormalizeEmail(email);
            string name = (fullName ?? string.Empty).Trim();
            string normalizedPhone = (phone ?? string.Empty).Trim();

            if (!IsValidEmail(normalizedEmail))
            {
                throw Fail(ApiErrorCodes.ValidationError, "Enter a valid email address.");
            }

            if (string.IsNullOrEmpty(password) || password.Length < 8)
            {
                throw Fail(ApiErrorCodes.ValidationError, "Password must be at least 8 characters.");
            }

            if (name.Length < 2)
            {
                throw Fail(ApiErrorCodes.ValidationError, "Enter your full name.");
            }

            if (!IsValidPhone(normalizedPhone))
            {
                throw Fail(ApiErrorCodes.ValidationError, "Enter a valid phone number, e.g. +234 801 234 5678.");
            }

            if (_db.Users.Any(u => string.Equals(u.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase)))
            {
                throw Fail(ApiErrorCodes.EmailTaken, "An account with that email already exists.");
            }

            var user = new User
            {
                Id = NewId("usr"),
                Email = normalizedEmail,
                FullName = name,
                Phone = normalizedPhone,
                Role = UserRole.Customer,
                DefaultStationId = _db.Stations.FirstOrDefault()?.Id,
                CreatedAt = NowIso()
            };

            _db.Users.Add(user);
            _db.Credentials.Add(BuildCredential(user.Id, password));
            _db.Carts[user.Id] = new List<CartLine>();

            string token = CreateSession(user);
            Save();
            SessionStore.Token = token;

            return new AuthResult { Token = token, User = user };
        }

        private AuthResult LoginInternal(string email, string password)
        {
            string normalizedEmail = NormalizeEmail(email);
            User user = _db.Users.FirstOrDefault(u =>
                string.Equals(u.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase));

            CredentialRecord credential = user == null
                ? null
                : _db.Credentials.FirstOrDefault(c => c.UserId == user.Id);

            if (user == null || credential == null || !VerifyPassword(password ?? string.Empty, credential))
            {
                throw Fail(ApiErrorCodes.Unauthorized, "Email or password is incorrect.");
            }

            string token = CreateSession(user);
            Save();
            SessionStore.Token = token;

            return new AuthResult { Token = token, User = user };
        }

        private User UpdateMeInternal(string fullName, string phone, string defaultStationId)
        {
            User user = RequireUser();
            string name = (fullName ?? string.Empty).Trim();
            string normalizedPhone = (phone ?? string.Empty).Trim();

            if (name.Length < 2)
            {
                throw Fail(ApiErrorCodes.ValidationError, "Enter your full name.");
            }

            if (!IsValidPhone(normalizedPhone))
            {
                throw Fail(ApiErrorCodes.ValidationError, "Enter a valid phone number, e.g. +234 801 234 5678.");
            }

            string stationId = string.IsNullOrWhiteSpace(defaultStationId) ? null : defaultStationId.Trim();
            if (stationId != null && _db.Stations.All(s => s.Id != stationId))
            {
                throw Fail(ApiErrorCodes.ValidationError, "Choose one of the four pickup stations.");
            }

            if (user.Role == UserRole.Customer && stationId == null)
            {
                throw Fail(ApiErrorCodes.ValidationError, "Customers need a default pickup station.");
            }

            user.FullName = name;
            user.Phone = normalizedPhone;
            user.DefaultStationId = stationId;
            Save();
            return user;
        }

        private IReadOnlyList<CartLine> UpdateCartInternal(IReadOnlyList<CartLine> lines)
        {
            User user = RequireCustomer();
            if (lines == null)
            {
                throw Fail(ApiErrorCodes.ValidationError, "Cart lines are required.");
            }

            var merged = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (CartLine line in lines)
            {
                if (line == null || string.IsNullOrWhiteSpace(line.ProductId))
                {
                    throw Fail(ApiErrorCodes.ValidationError, "Every cart line needs a product.");
                }

                if (line.Qty < 0)
                {
                    throw Fail(ApiErrorCodes.ValidationError, "Quantities cannot be negative.");
                }

                Product product = _db.Products.FirstOrDefault(p => p.Id == line.ProductId);
                if (product == null)
                {
                    throw Fail(ApiErrorCodes.ValidationError, "A product in your cart is no longer available.");
                }

                merged.TryGetValue(product.Id, out int existing);
                int qty = existing + line.Qty;
                if (qty > product.StockQty)
                {
                    throw Fail(ApiErrorCodes.InsufficientStock,
                        $"Only {product.StockQty} {product.Unit} of {product.Name} left.");
                }

                merged[product.Id] = qty;
            }

            var cart = merged
                .Where(pair => pair.Value > 0)
                .Select(pair => new CartLine { ProductId = pair.Key, Qty = pair.Value })
                .ToList();

            _db.Carts[user.Id] = cart;
            Save();
            return CloneCart(cart);
        }

        private Order PlaceOrderInternal(string stationId, OrderUrgency urgency)
        {
            User user = RequireCustomer();

            PickupStation station = _db.Stations.FirstOrDefault(s => s.Id == stationId);
            if (station == null)
            {
                throw Fail(ApiErrorCodes.ValidationError, "Choose a pickup station.");
            }

            List<CartLine> cart = GetCartFor(user.Id);
            if (cart.Count == 0)
            {
                throw Fail(ApiErrorCodes.ValidationError, "Your cart is empty.");
            }

            var lines = new List<OrderLine>();
            long totalKobo = 0;

            foreach (CartLine line in cart)
            {
                Product product = _db.Products.FirstOrDefault(p => p.Id == line.ProductId);
                if (product == null)
                {
                    throw Fail(ApiErrorCodes.ValidationError, "A product in your cart is no longer available.");
                }

                if (line.Qty <= 0)
                {
                    throw Fail(ApiErrorCodes.ValidationError, "Quantities must be at least 1.");
                }

                if (line.Qty > product.StockQty)
                {
                    throw Fail(ApiErrorCodes.InsufficientStock,
                        $"Only {product.StockQty} {product.Unit} of {product.Name} left.");
                }

                lines.Add(new OrderLine
                {
                    ProductId = product.Id,
                    Name = product.Name,
                    UnitPriceKobo = product.PriceKobo,
                    Qty = line.Qty
                });

                totalKobo += product.PriceKobo * line.Qty;
            }

            foreach (OrderLine line in lines)
            {
                Product product = _db.Products.First(p => p.Id == line.ProductId);
                product.StockQty -= line.Qty;
            }

            var order = new Order
            {
                Id = NewId("ord"),
                PublicCode = NewPublicCode(),
                CustomerId = user.Id,
                CustomerName = user.FullName,
                StationId = station.Id,
                Urgency = urgency,
                Status = OrderStatus.Placed,
                Lines = lines,
                TotalKobo = totalKobo,
                RiderId = null,
                CreatedAt = NowIso(),
                UpdatedAt = NowIso()
            };

            _db.Orders.Add(order);
            _db.Carts[user.Id] = new List<CartLine>();
            Save();
            return order;
        }

        private Order TransitionOrderInternal(string orderId, OrderStatus status, string riderId)
        {
            User user = RequireUser();
            Order order = FindOrder(orderId);

            bool isOperator = user.Role == UserRole.Operator;
            bool isAssignedRider = user.Role == UserRole.Rider && order.RiderId == user.Id;

            if (!isOperator && !isAssignedRider)
            {
                throw Fail(ApiErrorCodes.Unauthorized, "You cannot move this order.");
            }

            if (isOperator && status != OrderStatus.Confirmed
                           && status != OrderStatus.Packed
                           && status != OrderStatus.AssignedToRider)
            {
                throw Fail(ApiErrorCodes.Unauthorized, "Operators cannot move an order to that status.");
            }

            if (isAssignedRider && status != OrderStatus.InTransit && status != OrderStatus.ArrivedAtPickup)
            {
                throw Fail(ApiErrorCodes.Unauthorized, "Riders cannot move an order to that status.");
            }

            if (!IsLegalOrderTransition(order.Status, status))
            {
                throw Fail(ApiErrorCodes.IllegalTransition,
                    $"Cannot move {order.PublicCode} from {order.Status} to {status}.");
            }

            if (status == OrderStatus.AssignedToRider)
            {
                if (string.IsNullOrWhiteSpace(riderId))
                {
                    throw Fail(ApiErrorCodes.ValidationError, "Choose a rider to assign.");
                }

                User rider = _db.Users.FirstOrDefault(u => u.Id == riderId && u.Role == UserRole.Rider);
                if (rider == null)
                {
                    throw Fail(ApiErrorCodes.ValidationError, "That rider could not be found.");
                }

                order.RiderId = rider.Id;
            }

            order.Status = status;
            order.UpdatedAt = NowIso();
            Save();
            return order;
        }

        private StorageRequest TransitionStorageRequestInternal(string storageRequestId, StorageStatus status)
        {
            User user = RequireUser();
            StorageRequest request = _db.StorageRequests.FirstOrDefault(r => r.Id == storageRequestId);
            if (request == null)
            {
                throw Fail(ApiErrorCodes.ValidationError, "Storage request not found.");
            }

            bool allowed = false;

            if (user.Role == UserRole.Operator)
            {
                allowed = (status == StorageStatus.Approved && request.Status == StorageStatus.Requested)
                          || (status == StorageStatus.Stored && request.Status == StorageStatus.Approved)
                          || (status == StorageStatus.Released && request.Status == StorageStatus.ReleaseRequested);
            }
            else if (user.Role == UserRole.Customer && request.CustomerId == user.Id)
            {
                allowed = status == StorageStatus.ReleaseRequested && request.Status == StorageStatus.Stored;
            }

            if (!allowed)
            {
                if (user.Role == UserRole.Rider)
                {
                    throw Fail(ApiErrorCodes.Unauthorized, "Riders cannot change storage requests.");
                }

                if (user.Role == UserRole.Customer && request.CustomerId != user.Id)
                {
                    throw Fail(ApiErrorCodes.Unauthorized, "This storage request belongs to another customer.");
                }

                throw Fail(ApiErrorCodes.IllegalTransition,
                    $"Cannot move storage request from {request.Status} to {status}.");
            }

            request.Status = status;
            Save();
            return request;
        }

        private static bool IsLegalOrderTransition(OrderStatus from, OrderStatus to)
        {
            return from switch
            {
                OrderStatus.Placed => to == OrderStatus.Confirmed,
                OrderStatus.Confirmed => to == OrderStatus.Packed,
                OrderStatus.Packed => to == OrderStatus.AssignedToRider,
                OrderStatus.AssignedToRider => to == OrderStatus.InTransit,
                OrderStatus.InTransit => to == OrderStatus.ArrivedAtPickup,
                OrderStatus.ArrivedAtPickup => to == OrderStatus.Received,
                _ => false
            };
        }

        private User RequireUser()
        {
            string token = SessionStore.Token;
            if (string.IsNullOrWhiteSpace(token))
            {
                throw Fail(ApiErrorCodes.Unauthorized, "Sign in to continue.");
            }

            SessionRecord session = _db.Sessions.FirstOrDefault(s => s.Token == token);
            if (session == null)
            {
                throw Fail(ApiErrorCodes.Unauthorized, "Your session has expired. Sign in again.");
            }

            User user = _db.Users.FirstOrDefault(u => u.Id == session.UserId);
            if (user == null)
            {
                throw Fail(ApiErrorCodes.Unauthorized, "Your account could not be found.");
            }

            return user;
        }

        private User RequireCustomer()
        {
            User user = RequireUser();
            if (user.Role != UserRole.Customer)
            {
                throw Fail(ApiErrorCodes.Unauthorized, "This action is only available to customers.");
            }

            return user;
        }

        private Order FindOrder(string orderId)
        {
            Order order = _db.Orders.FirstOrDefault(o => o.Id == orderId);
            if (order == null)
            {
                throw Fail(ApiErrorCodes.ValidationError, "Order not found.");
            }

            return order;
        }

        private List<CartLine> GetCartFor(string userId)
        {
            if (!_db.Carts.TryGetValue(userId, out List<CartLine> cart) || cart == null)
            {
                cart = new List<CartLine>();
                _db.Carts[userId] = cart;
            }

            return cart;
        }

        private static List<CartLine> CloneCart(IEnumerable<CartLine> lines)
        {
            return lines
                .Select(line => new CartLine { ProductId = line.ProductId, Qty = line.Qty })
                .ToList();
        }

        private string CreateSession(User user)
        {
            string token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
            _db.Sessions.Add(new SessionRecord
            {
                Token = token,
                UserId = user.Id,
                CreatedAt = NowIso()
            });

            return token;
        }

        private string NewPublicCode()
        {
            for (int i = 0; i < 40; i++)
            {
                string code = "CR-" + Guid.NewGuid().ToString("N").Substring(0, 4).ToUpperInvariant();
                if (_db.Orders.All(o => o.PublicCode != code))
                {
                    return code;
                }
            }

            return "CR-" + Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant();
        }

        private static string NewId(string prefix)
        {
            return prefix + "-" + Guid.NewGuid().ToString("N").Substring(0, 10);
        }

        private void EnsureLoaded()
        {
            if (_db != null)
            {
                return;
            }

            try
            {
                if (File.Exists(_path))
                {
                    string json = File.ReadAllText(_path);
                    _db = JsonConvert.DeserializeObject<Database>(json, JsonSettings);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[ColdRoom] Could not read mock database: {exception.Message}");
                _db = null;
            }

            if (_db == null)
            {
                _db = new Database();
                Seed(_db);
                Save();
                return;
            }

            Normalize(_db);

            bool changed = false;
            if (_db.Stations.Count == 0)
            {
                SeedStations(_db);
                changed = true;
            }

            if (_db.Products.Count == 0)
            {
                SeedProducts(_db);
                changed = true;
            }

            changed |= SeedUsers(_db);

            if (changed)
            {
                Save();
            }
        }

        private void Normalize(Database db)
        {
            db.Users ??= new List<User>();
            db.Credentials ??= new List<CredentialRecord>();
            db.Products ??= new List<Product>();
            db.Stations ??= new List<PickupStation>();
            db.Carts ??= new Dictionary<string, List<CartLine>>();
            db.Orders ??= new List<Order>();
            db.StorageRequests ??= new List<StorageRequest>();
            db.Sessions ??= new List<SessionRecord>();
        }

        private void Seed(Database db)
        {
            SeedStations(db);
            SeedProducts(db);
            SeedUsers(db);
        }

        private static void SeedStations(Database db)
        {
            db.Stations.AddRange(new[]
            {
                new PickupStation { Id = "st-lekki", Name = "Lekki Phase 1", Area = "Island" },
                new PickupStation { Id = "st-yaba", Name = "Yaba", Area = "Mainland" },
                new PickupStation { Id = "st-ikeja", Name = "Ikeja", Area = "Mainland" },
                new PickupStation { Id = "st-surulere", Name = "Surulere", Area = "Mainland" }
            });
        }

        private static void SeedProducts(Database db)
        {
            AddProduct(db, "pr-chicken", "Frozen chicken", ProductCategory.Poultry, "kg", 3800, 40, true, OrderUrgency.Standard);
            AddProduct(db, "pr-beef", "Beef stew cut", ProductCategory.Meat, "kg", 5200, 26, true, OrderUrgency.Standard);
            AddProduct(db, "pr-goat", "Goat meat", ProductCategory.Meat, "kg", 6500, 18, true, OrderUrgency.Priority);
            AddProduct(db, "pr-catfish", "Catfish", ProductCategory.Fish, "kg", 4200, 32, true, OrderUrgency.Urgent);
            AddProduct(db, "pr-turkey", "Turkey parts", ProductCategory.Poultry, "kg", 4800, 24, true, OrderUrgency.Priority);
            AddProduct(db, "pr-ugu", "Ugu", ProductCategory.Vegetables, "pack", 800, 60, true, OrderUrgency.Urgent);
            AddProduct(db, "pr-tomatoes", "Tomatoes", ProductCategory.Vegetables, "kg", 1500, 45, true, OrderUrgency.Urgent);
            AddProduct(db, "pr-peppers", "Peppers", ProductCategory.Vegetables, "pack", 1200, 35, true, OrderUrgency.Priority);
            AddProduct(db, "pr-spinach", "Spinach", ProductCategory.Vegetables, "pack", 700, 50, true, OrderUrgency.Urgent);
            AddProduct(db, "pr-snails", "Fresh snails", ProductCategory.Other, "pack", 7500, 12, true, OrderUrgency.Priority);
        }

        private static bool SeedUsers(Database db)
        {
            bool changed = false;
            changed |= AddSeedUser(db, "usr-customer", "customer@coldroom.test", "ColdRoom123!",
                "Tola Adeyemi", "+234 801 234 5678", UserRole.Customer, "st-lekki");
            changed |= AddSeedUser(db, "usr-operator", "operator@coldroom.test", "ColdRoom123!",
                "Ngozi Eze", "+234 802 345 6789", UserRole.Operator, null);
            changed |= AddSeedUser(db, "usr-rider", "rider@coldroom.test", "ColdRoom123!",
                "Seye Ogun", "+234 803 456 7890", UserRole.Rider, null);
            return changed;
        }

        private static bool AddSeedUser(
            Database db,
            string id,
            string email,
            string password,
            string fullName,
            string phone,
            UserRole role,
            string stationId)
        {
            if (db.Users.Any(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            var user = new User
            {
                Id = id,
                Email = email,
                FullName = fullName,
                Phone = phone,
                Role = role,
                DefaultStationId = stationId,
                CreatedAt = DateTime.UtcNow.AddDays(-30).ToString("o", CultureInfo.InvariantCulture)
            };

            db.Users.Add(user);
            db.Credentials.Add(BuildCredential(user.Id, password));
            db.Carts[user.Id] = new List<CartLine>();
            return true;
        }

        private static void AddProduct(
            Database db,
            string id,
            string name,
            ProductCategory category,
            string unit,
            long naira,
            int stock,
            bool perishable,
            OrderUrgency urgency)
        {
            db.Products.Add(new Product
            {
                Id = id,
                Name = name,
                Category = category,
                Unit = unit,
                PriceKobo = naira * 100L,
                StockQty = stock,
                IsPerishable = perishable,
                RecommendedUrgency = urgency
            });
        }

        private static CredentialRecord BuildCredential(string userId, string password)
        {
            byte[] salt = new byte[16];
            using (RandomNumberGenerator random = RandomNumberGenerator.Create())
            {
                random.GetBytes(salt);
            }

            using var derive = new Rfc2898DeriveBytes(password, salt, HashIterations, HashAlgorithmName.SHA256);
            return new CredentialRecord
            {
                UserId = userId,
                Salt = Convert.ToBase64String(salt),
                Hash = Convert.ToBase64String(derive.GetBytes(32)),
                Iterations = HashIterations
            };
        }

        private static bool VerifyPassword(string password, CredentialRecord credential)
        {
            if (credential == null
                || string.IsNullOrEmpty(credential.Salt)
                || string.IsNullOrEmpty(credential.Hash))
            {
                return false;
            }

            byte[] salt;
            byte[] expected;
            try
            {
                salt = Convert.FromBase64String(credential.Salt);
                expected = Convert.FromBase64String(credential.Hash);
            }
            catch (FormatException)
            {
                return false;
            }

            int iterations = credential.Iterations > 0 ? credential.Iterations : HashIterations;
            using var derive = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
            byte[] actual = derive.GetBytes(expected.Length);
            return FixedTimeEquals(actual, expected);
        }

        private static bool FixedTimeEquals(byte[] left, byte[] right)
        {
            if (left == null || right == null || left.Length != right.Length)
            {
                return false;
            }

            int difference = 0;
            for (int i = 0; i < left.Length; i++)
            {
                difference |= left[i] ^ right[i];
            }

            return difference == 0;
        }

        private static string NormalizeEmail(string email)
        {
            return (email ?? string.Empty).Trim().ToLowerInvariant();
        }

        private static bool IsValidEmail(string email)
        {
            return !string.IsNullOrWhiteSpace(email)
                   && email.Length <= 254
                   && EmailPattern.IsMatch(email);
        }

        private static bool IsValidPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone) || !PhonePattern.IsMatch(phone))
            {
                return false;
            }

            int digits = phone.Count(char.IsDigit);
            return digits >= 10 && digits <= 15;
        }

        private static string NowIso()
        {
            return DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
        }

        private static ApiException Fail(string code, string message)
        {
            return new ApiException(code, message);
        }

        private void Save()
        {
            try
            {
                string directory = Path.GetDirectoryName(_path);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(_path, JsonConvert.SerializeObject(_db, JsonSettings));
            }
            catch (Exception exception)
            {
                Debug.LogError($"[ColdRoom] Could not save mock database: {exception.Message}");
            }
        }

        private sealed class Database
        {
            public List<User> Users { get; set; } = new();
            public List<CredentialRecord> Credentials { get; set; } = new();
            public List<Product> Products { get; set; } = new();
            public List<PickupStation> Stations { get; set; } = new();
            public Dictionary<string, List<CartLine>> Carts { get; set; } = new();
            public List<Order> Orders { get; set; } = new();
            public List<StorageRequest> StorageRequests { get; set; } = new();
            public List<SessionRecord> Sessions { get; set; } = new();
        }

        private sealed class CredentialRecord
        {
            public string UserId { get; set; }
            public string Salt { get; set; }
            public string Hash { get; set; }
            public int Iterations { get; set; }
        }

        private sealed class SessionRecord
        {
            public string Token { get; set; }
            public string UserId { get; set; }
            public string CreatedAt { get; set; }
        }
    }
}
