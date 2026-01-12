using System.ComponentModel.DataAnnotations;

namespace OpcSubscriptionService.Models
{
    public class Incident
    {
        [Key]
        public int Id { get; set; }
        public string NodeName { get; set; }
        public string? Discription { get; set; }
        public string Status { get; set; }
        public bool IsClosed { get; set; } = false;
        public DateTime CreatedAt { get; set; }
        public DateTime ClosedAt { get; set; }
        public int HardwareId { get; set; }
        public string HardwareName { get; set; }
        public int ControlBlockId { get; set; }
        public int ObjectId { get; set; }
        public int ServiceUserId { get; set; }
    }
}
