using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI;
using Nova.Core;

namespace Nova.Sc.Fields.Templated.Table
{
    public class Table : Control
    {
        public Table()
        {

        }

        public Table(IEnumerable<Row> rows)
        {
            foreach(var row in rows)
            {
                Controls.Add(row);
            }
        }

        protected override void RenderChildren(HtmlTextWriter writer)
        {
            writer.Write(string.Format("<table id=\"{0}\"{1} width=\"100%\" cellpadding=\"4\" cellspacing=\"0\" border=\"0\">", 
                this.ID, 
                string.IsNullOrEmpty(CssClass) ? "" : (" class=\"" + CssClass + "\"")));
            base.RenderChildren(writer);
            writer.Write("</table>");
        }

        public void AddRow(Row row)
        {
            Controls.Add(row);
        }

        public void AddRow(IEnumerable<Row> rows)
        {
            foreach (var row in rows)
            {
                Controls.Add(row);
            }
        }

        public IEnumerable<Row> Rows
        {
            get
            {
                return Controls.Filter<Row>();
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
    }
}
