using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ColdRoom.Core.Models;

namespace ColdRoom.Api
{
    /// <summary>
    /// Placeholder for the hosted backend. Every call fails until the REST client is built.
    /// </summary>
    public sealed class RestColdRoomApi : IColdRoomApi
    {
        private readonly string _baseUrl;

        public RestColdRoomApi(string baseUrl)
        {
            _baseUrl = baseUrl;
        }

        public string BaseUrl => _baseUrl;

        public Task<AuthResult> Register(string email, string password, string fullName, string phone) => NotReady<AuthResult>();
        public Task<AuthResult> Login(string email, string password) => NotReady<AuthResult>();
        public Task Logout() => NotReady<bool>();
        public Task<User> GetMe() => NotReady<User>();
        public Task<User> UpdateMe(string fullName, string phone, string defaultStationId) => NotReady<User>();
        public Task<IReadOnlyList<Product>> GetProducts(ProductCategory? category = null) => NotReady<IReadOnlyList<Product>>();
        public Task<IReadOnlyList<PickupStation>> GetStations() => NotReady<IReadOnlyList<PickupStation>>();
        public Task<IReadOnlyList<CartLine>> GetCart() => NotReady<IReadOnlyList<CartLine>>();
        public Task<IReadOnlyList<CartLine>> UpdateCart(IReadOnlyList<CartLine> lines) => NotReady<IReadOnlyList<CartLine>>();
        public Task<Order> PlaceOrder(string stationId, OrderUrgency urgency) => NotReady<Order>();
        public Task<IReadOnlyList<Order>> GetOrders() => NotReady<IReadOnlyList<Order>>();
        public Task<Order> GetOrder(string orderId) => NotReady<Order>();
        public Task<Order> TransitionOrder(string orderId, OrderStatus status, string riderId = null) => NotReady<Order>();
        public Task<Order> ConfirmReceipt(string orderId) => NotReady<Order>();
        public Task<StorageRequest> CreateStorageRequest(string description, double estimatedKg) => NotReady<StorageRequest>();
        public Task<IReadOnlyList<StorageRequest>> GetStorageRequests() => NotReady<IReadOnlyList<StorageRequest>>();
        public Task<StorageRequest> TransitionStorageRequest(string storageRequestId, StorageStatus status) => NotReady<StorageRequest>();
        public Task<IReadOnlyList<User>> GetRiders() => NotReady<IReadOnlyList<User>>();

        private static Task<T> NotReady<T>()
        {
            return Task.FromException<T>(new NotSupportedException(
                "The REST API is not implemented yet. Enable useMockApi in AppConfig."));
        }
    }
}
