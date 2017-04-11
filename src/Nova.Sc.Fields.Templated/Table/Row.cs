using Nova.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI;

namespace Nova.Sc.Fields.Templated.Table
{
    public class Row : Control
    {
        public Row()
        {

        }

        public string Key
        {
            get
            {
                this.TrackViewState();
                return this.ViewState["rowKey"] as string;
            }
            set
            {
                this.TrackViewState();
                this.ViewState["rowKey"] = value;
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

        public Row(IEnumerable<Cell> cells)
        {
            foreach (var cell in cells)
            {
                Controls.Add(cell);
            }
        }

        protected override void RenderChildren(HtmlTextWriter writer)
        {
            writer.Write("<tr" + (string.IsNullOrEmpty(CssClass) ? "" : (" class=\"" + CssClass + "\"")) + ">");
            base.RenderChildren(writer);
            writer.Write("</tr>");
        }

        public void AddCell(Cell cell)
        {
            Controls.Add(cell);
        }

        public IEnumerable<Cell> Cells
        {
            get
            {
                return Controls.Filter<Cell>();
            }
        }
    }
}
