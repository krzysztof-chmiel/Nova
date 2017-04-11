using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI;

namespace Nova.Sc.Fields.Templated.Table
{
    public class LiteralWithViewState : Control
    {
        public LiteralWithViewState()
        {
        }

        public LiteralWithViewState(string text)
        {
            this.Text = text;
        }

        public string Text
        {
            get
            {
                this.TrackViewState();
                return this.ViewState["Text"] as string;
            }

            set
            {
                this.TrackViewState();
                this.ViewState["Text"] = value;
            }
        }

        protected override void Render(HtmlTextWriter writer)
        {
            writer.Write(Text);
        }
    }
}
