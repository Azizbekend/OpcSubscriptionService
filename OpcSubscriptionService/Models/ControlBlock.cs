using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace OpcSubscriptionService.Models
{
    public  class ControlBlock
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string PlcIpAdress { get; set; } = string.Empty;
        public int StaticObjectInfoId { get; set; }
    }
}
