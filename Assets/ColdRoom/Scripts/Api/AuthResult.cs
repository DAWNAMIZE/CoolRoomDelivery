using ColdRoom.Core.Models;

namespace ColdRoom.Api
{
    public sealed class AuthResult
    {
        public string Token { get; set; }
        public User User { get; set; }
    }
}
