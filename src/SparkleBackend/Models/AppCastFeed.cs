using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Hasseware.SparkleService.Models
{
    public class AppCastFeed : Collection<AppCastItem>
    {
        [Required, Range(3, 63)]
        public string Title { get; set; }

        public string Link { get; set; }

        public string Description { get; set; }

        public CultureInfo Language { get; set; }
    }
}