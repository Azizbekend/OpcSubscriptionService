using Microsoft.EntityFrameworkCore;
using Opc.Ua;
using OpcSubscriptionService.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpcSubscriptionService
{
    public class IncidentRepo
    {
        private readonly AppDbContext _context;

        public IncidentRepo(AppDbContext context)
        {
            _context = context;
        }

        public async Task CreateHardwareModeEvent(string nodeId, string value)
        {
            var node = await _context.Nodes.FirstOrDefaultAsync(x => x.PlcNodeId == nodeId);
            var hardware = await _context.Hardwares.FirstOrDefaultAsync(x => x.Id == node.HardwareId);
            var hardwareEvent = new HardwareEvent()
            {
                Discription = $"{node.Name} изменён на {value}",
                HardwareId = hardware.Id,
                UserId = 0

            };
            var check = await _context.EventsJournal.Where(x => x.HardwareId == hardware.Id && x.Discription == hardwareEvent.Discription).OrderByDescending(x => x.Id).FirstOrDefaultAsync();
            if(check == null)
            {   _context.EventsJournal.Add(hardwareEvent);
                await _context.SaveChangesAsync();
            }
        }
        public async Task CreateIncidentAsync(string nodeId)
        {
            var node = await _context.Nodes.FirstOrDefaultAsync(x => x.PlcNodeId == nodeId);
            var hardware = await _context.Hardwares.FirstOrDefaultAsync(x => x.Id == node.HardwareId);
            var incidentNodes = await _context.Nodes
               .Where(n => n.HardwareId == hardware.Id && (n.PlcNodeId.EndsWith("hAlmAi") || n.PlcNodeId.EndsWith("hAlmQF") || n.PlcNodeId.EndsWith("hAlmStator") ||
               n.PlcNodeId.EndsWith("hAlmVentQF") || n.PlcNodeId.EndsWith("hAlmVentCmd") || n.PlcNodeId.EndsWith("hAlmDisconnect") ||
               n.PlcNodeId.EndsWith("hAlmFC") || n.PlcNodeId.EndsWith("hAlmKonc") || n.PlcNodeId.EndsWith("hAlmCmd")
               || n.PlcNodeId.EndsWith("hAlmMoment") || n.PlcNodeId.EndsWith("hAlmExt")))
               .OrderBy(n => n.Name)
               .FirstAsync();
            var block = await _context.ControlBlocks.FirstOrDefaultAsync(x => x.Id == hardware.ControlBlockId);
            var staticObject = await _context.StaticObjectInfos.FirstOrDefaultAsync(x => x.Id == block.StaticObjectInfoId);
            var incident = await _context.Incidents.FirstOrDefaultAsync(x => x.HardwareId == hardware.Id &&  x.ControlBlockId == block.Id && x.ObjectId == staticObject.Id && !x.IsClosed && x.NodeName == incidentNodes.Name);
            if (incident == null) 
            {
                var newIncident = new Incident
                {
                    NodeName = incidentNodes.Name,
                    HardwareId = hardware.Id,
                    HardwareName = hardware.Name,
                    ControlBlockId = block.Id,
                    ObjectId = staticObject.Id,
                    Status = "New",
                    CreatedAt = DateTime.UtcNow
                };
                var hEvent = new HardwareEvent
                {
                    Discription = $"{hardware.Name}. {incidentNodes.Name}",
                    TimeStamp = DateTime.Now.ToUniversalTime(),
                    HardwareId = hardware.Id                    
                };
                _context.EventsJournal.Add(hEvent);
                _context.Incidents.Add(newIncident);
                await _context.SaveChangesAsync();
            }

        }


    }
}
