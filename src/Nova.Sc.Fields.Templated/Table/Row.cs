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

        public Row(IEnumerable<Cell> cells)
        {
            foreach (var cell in cells)
            {
                Controls.Add(cell);
            }
        }

        protected override void RenderChildren(HtmlTextWriter writer)
        {
            writer.Write("<tr>");
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
                foreach (Cell c in Controls)
                {
                    yield return c;
                }
            }
        }
    }
}
