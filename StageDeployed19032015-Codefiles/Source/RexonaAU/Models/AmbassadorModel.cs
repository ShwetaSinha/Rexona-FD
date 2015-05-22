using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RexonaAU.Models
{
    public class AmbassadorModel
    {
        public string AmbassadorName { get; set; }
        public string AmbassadorURL { get; set; }
        public int AmbassadorId { get; set; }
        public string AmbassadorImage { get; set; }
        public string AmbassadorGoal { get; set; }
        public string AmbassadorDescription { get; set; }
        public DateTime createdDate { get; set; }
    }
}