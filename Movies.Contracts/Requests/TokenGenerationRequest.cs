using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Movies.Contracts.Requests
{
    public class TokenGenerationRequest
    {
        public Guid UserId { get; set; }

        public string Email { get; set; }

        public Dictionary<string, object> CustomClaims { get; set; } = new();
    }
}
