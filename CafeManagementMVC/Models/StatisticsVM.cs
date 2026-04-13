namespace CafeManagementMVC.Models
{
    public class StatisticsVM
    {
        public List<string> Labels { get; set; }
        public List<decimal> Data { get; set; }
        public List<TopProductVM> TopProducts { get; set; }
        // Thêm các thuộc tính tổng quan để hiển thị ở các thẻ màu
        public decimal TotalRevenue => Data.Sum();
    }

    public class TopProductVM
    {
        public string ProductName { get; set; }
        public int TotalSold { get; set; }
    }
}