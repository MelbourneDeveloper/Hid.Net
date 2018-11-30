namespace Hid.Net
{
    public class VendorProductIdPair
    {
        public int VendorId { get; }
        public int ProductId { get; }

        public VendorProductIdPair(int vendorId, int productId)
        {
            VendorId = vendorId;
            ProductId = productId;
        }
    }
}
