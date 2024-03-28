namespace DurableOrchestrator.Examples.Invoices;

public class Invoice
{
    [JsonPropertyName("company_name")] public string? CompanyName { get; set; }

    [JsonPropertyName("invoice_date")] public DateTime? InvoiceDate { get; set; }

    [JsonPropertyName("products")] public IEnumerable<InvoiceProduct>? Products { get; set; }

    [JsonPropertyName("total_amount")] public double? TotalAmount { get; set; }

    [JsonPropertyName("signatures")] public IEnumerable<InvoiceSignature>? Signatures { get; set; }

    public class InvoiceProduct
    {
        [JsonPropertyName("id")] public string? Id { get; set; }

        [JsonPropertyName("unit_price")] public double? UnitPrice { get; set; }

        [JsonPropertyName("quantity")] public double Quantity { get; set; }

        [JsonPropertyName("total")] public double? Total { get; set; }
    }

    public class InvoiceSignature
    {
        [JsonPropertyName("type")] public string? Type { get; set; }

        [JsonPropertyName("has_signature")] public bool? HasSignature { get; set; }

        [JsonPropertyName("signed_on")] public DateTime? SignedOn { get; set; }
    }

    public static Invoice Empty => new()
    {
        CompanyName = string.Empty,
        InvoiceDate = DateTime.MinValue,
        Products = new List<InvoiceProduct>
        {
            new() { Id = string.Empty, UnitPrice = 0.0, Quantity = 0.0, Total = 0.0 }
        },
        TotalAmount = 0,
        Signatures = new List<InvoiceSignature>
        {
            new() { Type = string.Empty, HasSignature = false, SignedOn = DateTime.MinValue }
        }
    };
}
