﻿using System;
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

        public Cell(Control innerControl)
        {
            Controls.Add(innerControl);
        }

        protected override void RenderChildren(HtmlTextWriter writer)
        {
            writer.Write("<td>");
            base.RenderChildren(writer);
            writer.Write("</td>");
        }
    }
}
