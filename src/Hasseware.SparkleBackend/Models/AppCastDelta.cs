using System;
using System.ComponentModel.DataAnnotations;

namespace Hasseware.SparkleService.Models
{
    public class AppCastDelta : AppCastEnclosure
    {
        [Required]
        public Version DeltaFrom { get; set; }
    }
}
