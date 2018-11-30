namespace Hid.Net
{
    public class VendorIdAndProductId
    {
        public int VendorId { get; }
        public int ProductId { get; }

        public VendorIdAndProductId(int vendorId, int productId)
        {
            VendorId = vendorId;
            ProductId = productId;
        }
    }
}
