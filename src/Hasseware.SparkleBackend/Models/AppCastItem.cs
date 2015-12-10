using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace Hasseware.SparkleService.Models
{
    public class AppCastItem : Collection<AppCastDelta>
    {
        [Required, Range(3, 63)]
        public string Title { get; set; }

        public string NotesLink { get; set; }

        public DateTime? Published { get; set; }

        public AppCastEnclosure Enclosure { get; set; }
    }
}