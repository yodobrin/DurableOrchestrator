namespace DurableOrchestrator.Examples.Invoices;

public class InvoiceEntity : Invoice
{
    /// <summary>
    /// Gets or sets the unique identifier for the tenant associated with the invoice.
    /// </summary>
    public required Guid TenantId { get; set; }

    /// <summary>
    /// Gets or sets the name of the invoice file processed.
    /// </summary>
    public required string InvoiceName { get; set; }

    public static InvoiceEntity FromInvoice(Guid tenantId, string invoiceName, Invoice invoice)
    {
        return new InvoiceEntity
        {
            TenantId = tenantId,
            InvoiceName = invoiceName,
            CompanyName = invoice.CompanyName,
            InvoiceDate = invoice.InvoiceDate,
            Products = invoice.Products,
            TotalAmount = invoice.TotalAmount,
            Signatures = invoice.Signatures
        };
    }
}
