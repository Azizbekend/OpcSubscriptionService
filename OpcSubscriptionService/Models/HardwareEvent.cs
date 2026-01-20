using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace OpcSubscriptionService.Models
{
    public class HardwareEvent
    {
        [Key]
        public int Id { get; set; }
        public string Discription { get; set; }
        public DateTime TimeStamp { get; set; } = DateTime.Now.ToUniversalTime();
        public int HardwareId { get; set; }
        public int UserId { get; set; }
    }
}
