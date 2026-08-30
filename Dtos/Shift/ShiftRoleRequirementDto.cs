namespace MenuGoBE.Dtos.Shift
{
    public class ShiftRoleRequirementDto
    {
        public long? Id { get; set; }
        public long ShiftId { get; set; }
        public long RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public int MinQuantity { get; set; } = 1;
    }
}
