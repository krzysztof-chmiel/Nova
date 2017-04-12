using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI;

namespace Nova.Web.UI
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

        public string CssClass
        {
            get
            {
                this.TrackViewState();
                return this.ViewState["CssClass"] as string;
            }
            set
            {
                this.TrackViewState();
                this.ViewState["CssClass"] = value;
            }
        }

        protected override void RenderChildren(HtmlTextWriter writer)
        {
            writer.Write("<td" + (string.IsNullOrEmpty(CssClass) ? "" : (" class=\"" + CssClass + "\"")) + ">");
            base.RenderChildren(writer);
            writer.Write("</td>");
        }
    }
}
