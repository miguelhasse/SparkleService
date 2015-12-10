using System;
using System.ComponentModel.DataAnnotations;

namespace Hasseware.SparkleService.Models
{
    public class AppCastEnclosure
    {
        public string ContentLink { get; set; }

        public string ContentType { get; set; }

        public long? ContentLength { get; set; }

        [Required]
        public Version Version { get; set; }

        public string Signature { get; set; }
    }
}
