using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace OpcSubscriptionService.Models
{
    public class Hardware
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public int ControlBlockId { get; set; }
    }
}
