using RexonaAU.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace RexonaAU.usercontrols
{
    public partial class PledgeTicker : System.Web.UI.UserControl
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            PledgeController pledge = new PledgeController();
            var tickerCount = pledge.PledgeTickerCount().Data;
            pledgeTickerSpan.InnerText = tickerCount.ToString();
        
        }
    }
}