using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI;

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
            writer.Write("<table width=\"100%\" cellpadding=\"4\" cellspacing=\"0\" border=\"0\">");
            base.RenderChildren(writer);
            writer.Write("</table>");
        }

        public void AddRow(Row row)
        {
            Controls.Add(row);
        }
    }
}
