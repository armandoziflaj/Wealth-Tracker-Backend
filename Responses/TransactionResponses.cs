namespace WealthTracker.Responses;

public class TransactionResponseDto
    {
        public long Id { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
        public int Type { get; set; } 
        public long CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string? CategoryIcon { get; set; }
        public string? CategoryColor { get; set; }
        public string? Notes { get; set; }
        public bool IsRecurring { get; set; }
    }