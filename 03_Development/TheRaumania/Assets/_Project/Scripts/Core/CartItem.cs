[System.Serializable]
public class CartItem
{
    public BaseItemSO itemData;
    public int quantity;
    public int priceAtPurchase;
    public int TotalPrice => quantity * priceAtPurchase;

    public CartItem(BaseItemSO data, int qty, int price)
    {
        itemData = data;
        quantity = qty;
        priceAtPurchase = price;
    }
}