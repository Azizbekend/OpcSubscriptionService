using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpcSubscriptionService
{
    public class NodeRepo
    {
        private readonly AppDbContext _context;

        public NodeRepo(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<string>> GetNodesIds()
        {
            var nodes = new List<string>();
            var nodesinfos = await _context.Nodes.ToListAsync();
            foreach (var nodeinfo in nodesinfos) 
            {
                nodes.Add(nodeinfo.PlcNodeId);
            }
            return nodes;
        }
    }
}
