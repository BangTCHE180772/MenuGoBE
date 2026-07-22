using MenuGoBE.Models.Enums;

namespace MenuGoBE.Helpers
{
    public static class BInventoryHelper
    {
        public static BInventoryType MapProductTypeToBInventoryType(string? productType)
        {
            if (string.IsNullOrWhiteSpace(productType))
                return BInventoryType.RawMaterial;

            return productType.Trim().ToLower() switch
            {
                "ingredient" => BInventoryType.RawMaterial,
                "processed" => BInventoryType.SemiFinished,
                "manufactured" => BInventoryType.Finished,
                "regular" => BInventoryType.Finished,
                "tool" => BInventoryType.Consumable,
                _ => BInventoryType.RawMaterial
            };
        }
    }
}
