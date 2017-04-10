using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI;

namespace Nova.Sc.Fields.Templated.Table
{
    public class Cell : Control
    {
        public Cell()
        {

        }

        public Cell(Control innerControl, string cellKey)
        {
            Controls.Add(innerControl);
            Key = cellKey;
        }

        public string Key
        {
            get
            {
                this.TrackViewState();
                return this.ViewState["cellKey"] as string;
            }
            set
            {
                this.TrackViewState();
                this.ViewState["cellKey"] = value;
            }
        }

        protected override void RenderChildren(HtmlTextWriter writer)
        {
            writer.Write("<td>");
            base.RenderChildren(writer);
            writer.Write("</td>");
        }
    }
}
