using Microsoft.EntityFrameworkCore;
using PetShop.Web.Data;

namespace PetShop.Web.Services;

public sealed class OrderService(ApplicationDbContext db, StoredValueService storedValue, TimeProvider timeProvider)
{
    public async Task<ChargeResult> CompleteGroomingAsync(Guid orderId, Guid groomerId, Guid? memberCardId,
        PaymentMethod externalMethod, string requestId, string operatorId, CancellationToken ct = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var order = await db.GroomingOrders.SingleOrDefaultAsync(x => x.Id == orderId, ct)
            ?? throw new InvalidOperationException("美容工单不存在。");
        if (order.Status is GroomingStatus.Cancelled or GroomingStatus.NoShow)
            throw new InvalidOperationException("已取消或爽约的工单不能结算。");
        if (order.Status == GroomingStatus.Completed)
        {
            var prior = await db.Payments.AsNoTracking().SingleAsync(x => x.DocumentType == DocumentType.Grooming && x.DocumentId == orderId, ct);
            return new(prior.Id, prior.PrincipalPaid, prior.BonusPaid, prior.ExternalPaid, true);
        }
        if (!await db.StaffMembers.AnyAsync(x => x.Id == groomerId && x.IsActive && x.IsGroomer, ct))
            throw new InvalidOperationException("必须选择一名在职美容师。");

        order.ActualGroomerId = groomerId;
        order.Status = GroomingStatus.Completed;
        order.CompletedAt = timeProvider.GetLocalNow();
        var result = await storedValue.ChargeAsync(DocumentType.Grooming, order.Id, memberCardId,
            order.AmountDue, externalMethod, requestId, operatorId, ct);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return result;
    }

    public async Task<ChargeResult> SellProductAsync(Guid productId, decimal quantity, Guid? customerId,
        Guid? memberCardId, PaymentMethod externalMethod, string requestId, string operatorId, CancellationToken ct = default)
    {
        if (quantity <= 0 || decimal.Round(quantity, 2) != quantity) throw new InvalidOperationException("销售数量必须大于零且最多两位小数。");
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var product = await db.Products.SingleOrDefaultAsync(x => x.Id == productId && x.IsActive, ct)
            ?? throw new InvalidOperationException("商品不存在或已停用。");
        if (product.StockQuantity < quantity) throw new InvalidOperationException("库存不足。");
        var now = timeProvider.GetLocalNow();
        var order = new RetailOrder
        {
            OrderNumber = $"LS{now:yyyyMMddHHmmssfff}", CustomerId = customerId,
            AmountDue = decimal.Round(product.SalePrice * quantity, 2), CreatedByUserId = operatorId, CreatedAt = now
        };
        order.Items.Add(new RetailOrderItem { ProductId = product.Id, ProductName = product.Name, Quantity = quantity,
            UnitPrice = product.SalePrice, LineTotal = order.AmountDue });
        product.StockQuantity -= quantity;
        db.RetailOrders.Add(order);
        db.InventoryTransactions.Add(new InventoryTransaction { ProductId = product.Id, Kind = InventoryKind.Sale,
            QuantityChange = -quantity, QuantityAfter = product.StockQuantity, SourceDocumentId = order.Id,
            OperatorUserId = operatorId, Note = order.OrderNumber, CreatedAt = now });
        var result = await storedValue.ChargeAsync(DocumentType.Retail, order.Id, memberCardId,
            order.AmountDue, externalMethod, requestId, operatorId, ct);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return result;
    }
}
