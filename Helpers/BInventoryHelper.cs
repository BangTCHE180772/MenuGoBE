using MenuGoBE.Models.Enums;

namespace MenuGoBE.Helpers
{
    public static class BInventoryHelper
    {
        public static BInventoryType MapProductTypeToBInventoryType(ProductType type)
        {
            return type switch
            {
                ProductType.Ingredient => BInventoryType.RawMaterial,
                ProductType.Processed => BInventoryType.SemiFinished,
                ProductType.Manufactured => BInventoryType.Finished,
                ProductType.Regular => BInventoryType.Finished,
                ProductType.Tool => BInventoryType.Consumable,
                _ => BInventoryType.RawMaterial
            };
        }

        public static BInventoryType MapProductTypeToBInventoryType(string? productType)
        {
            if (string.IsNullOrWhiteSpace(productType))
                return BInventoryType.RawMaterial;

            if (Enum.TryParse<ProductType>(productType, true, out var parsedType))
            {
                return MapProductTypeToBInventoryType(parsedType);
            }

            return BInventoryType.RawMaterial;
        }
    }
}
