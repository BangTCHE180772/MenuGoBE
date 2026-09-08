using System.ComponentModel.DataAnnotations;

namespace MenuGoBE.Dtos.Order
{
    public class ReuseLeftoverDto
    {
        [Required]
        public long LeftoverId { get; set; }
    }
}
