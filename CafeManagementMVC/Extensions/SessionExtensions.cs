using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace CafeManagementMVC.Extensions // Đổi tên namespace này nếu project bạn tên khác nhé
{
    public static class SessionExtensions
    {
        // Hàm Set dùng để lưu Object/List vào Session
        public static void Set<T>(this ISession session, string key, T value)
        {
            session.SetString(key, JsonSerializer.Serialize(value));
        }

        // Hàm Get dùng để lấy Object/List từ Session ra
        public static T Get<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
            return value == null ? default : JsonSerializer.Deserialize<T>(value);
        }
    }
}