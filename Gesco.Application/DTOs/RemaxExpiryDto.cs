using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gesco.Application.DTOs
{
    public class RemaxExpiryDto
    {
        public string MLSID { get; set; }
        public string? Status { get; set; }
        public long? ExpiryDate { get; set; }
        public long? ContractExpirationDate { get; set; }
    }
}
