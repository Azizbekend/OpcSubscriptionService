using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace OpcSubscriptionService.Models
{
    public class StaticObjectInfo
    {
        [Key]
        public int Id { get; set; }
        public string? Name { get; set; }
        public string Latitude { get; set; }
        public string Longitude { get; set; }
        public string Adress { get; set; }
    }
}
