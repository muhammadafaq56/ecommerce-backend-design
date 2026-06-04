namespace ECommerceApp.Areas.Admin.Models.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalProducts { get; set; }
        public int TotalCategories { get; set; }
        public int TotalOrders { get; set; }
        public int TotalUsers { get; set; }
        public decimal TotalRevenue { get; set; }
        public int PendingOrders { get; set; }
        public List<RecentOrderItem> RecentOrders { get; set; } = new();
    }

    public class RecentOrderItem
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public string CustomerEmail { get; set; } = "";
        public decimal Total { get; set; }
        public string OrderStatus { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }
}