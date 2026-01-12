using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpcSubscriptionService.Models
{
    public class NodeIndicates
    {
        [Key]
        public int Id { get; set; }
        public string Indicates { get; set; } //double
        public string PlcNodeId { get; set; }
        //[ForeignKey("NodeInfo")]
        //public int NodeInfoId { get; set; }
        //public NodeInfo NodeInfo { get; set; } = null!;
        public DateTime TimeStamp { get; set; } = DateTime.Now;
    }
}
