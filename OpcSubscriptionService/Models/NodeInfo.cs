using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace OpcSubscriptionService.Models
{
    public class NodeInfo
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Mesurement { get; set; } = string.Empty; //кв.ч, м3 и так далее
        public string PlcNodeId { get; set; }
        public bool IsCommand { get; set; } = false;
        public bool IsValue { get; set; } = true;
        [ForeignKey("HardwareInfo")]
        public int HardwareId { get; set; }


    }
}
