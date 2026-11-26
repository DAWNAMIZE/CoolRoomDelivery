using System.Collections.Generic;
using System.Threading.Tasks;
using ColdRoom.Core.Models;

namespace ColdRoom.Api
{
    /// <summary>
    /// The app talks to this contract only. The mock implementation persists locally;
    /// the REST implementation is reserved for the hosted backend.
    /// </summary>
    public interface IColdRoomApi
    {
        Task<AuthResult> Register(string email, string password, string fullName, string phone);
        Task<AuthResult> Login(string email, string password);
        Task Logout();
        Task<User> GetMe();
        Task<User> UpdateMe(string fullName, string phone, string defaultStationId);

        Task<IReadOnlyList<Product>> GetProducts(ProductCategory? category = null);
        Task<IReadOnlyList<PickupStation>> GetStations();

        Task<IReadOnlyList<CartLine>> GetCart();
        Task<IReadOnlyList<CartLine>> UpdateCart(IReadOnlyList<CartLine> lines);
        Task<Order> PlaceOrder(string stationId, OrderUrgency urgency);
        Task<IReadOnlyList<Order>> GetOrders();
        Task<Order> GetOrder(string orderId);
        Task<Order> TransitionOrder(string orderId, OrderStatus status, string riderId = null);
        Task<Order> ConfirmReceipt(string orderId);

        Task<StorageRequest> CreateStorageRequest(string description, double estimatedKg);
        Task<IReadOnlyList<StorageRequest>> GetStorageRequests();
        Task<StorageRequest> TransitionStorageRequest(string storageRequestId, StorageStatus status);

        Task<IReadOnlyList<User>> GetRiders();
    }
}
