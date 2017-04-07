using Sitecore;
using Sitecore.Diagnostics;
using Sitecore.Web.UI.HtmlControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI;
using Nova.Core;
using System.Runtime.Serialization;
using System.Web;
using System.Collections.Specialized;
using System.IO;
using Sitecore.Web.UI.Sheer;
using Sitecore.Shell.Applications.ContentEditor;

namespace Nova.Sc.Fields.Templated
{
    [UsedImplicitly]
    public class TemplatedList : Input
    {
        public TemplatedList()
        {
            Activation = true;
            Class = "scContentControl";
        }

        protected override void OnLoad(EventArgs e)
        {
            Assert.ArgumentNotNull(e, "e");
            base.OnLoad(e);
            if (Sitecore.Context.ClientPage.IsEvent)
            {
                LoadValue();
            }
            else
            {
                BuildControl();
            }
        }

        protected override void SetModified()
        {
            base.SetModified();
            if (TrackModified)
            {
                Sitecore.Context.ClientPage.Modified = true;
            }
        }

        private void LoadValue()
        {
            if (!ReadOnly && !Disabled)
            {
                DataValue valueObject = new DataValue();
                System.Web.UI.Page handler = HttpContext.Current.Handler as System.Web.UI.Page;
                NameValueCollection form = handler != null ? handler.Request.Form : new NameValueCollection();
                //ID_row_12_UNID_value
                string keyStart = ID + "_row_";
                string valueKeySuffix = "_value";
                foreach (string id in form.Keys)
                {
                    if ((!string.IsNullOrEmpty(id) && id.StartsWith(keyStart)) && !id.EndsWith(valueKeySuffix))
                    {
                        int row = int.Parse(id.Substring(keyStart.Length, id.Substring(keyStart.Length).IndexOf('_')));

                        DataItem dataItem = valueObject.items.FirstOrDefault(d => d.row == row);
                        if (dataItem == null)
                        {
                            dataItem = new DataItem() { row = row };
                            valueObject.items.Add(dataItem);
                        }
                        dataItem.properties.Add(new DataItemProperty() { key = form[id], value = form[id + valueKeySuffix] });
                    }
                }

                //TODO - deleted rows, update item count etc.
                valueObject.items = valueObject.items.Where(i => i.properties.Any(p => !string.IsNullOrEmpty(p.value))).OrderBy(i => i.row).ToList();
                if (Value != valueObject.JsonSerialize())
                {
                    ValueObject = valueObject;
                    SetModified();
                }
            }
        }

        private void BuildControl()
        {
            RowCount = 0;

            foreach (var row in ValueObject.items)
            {
                Controls.Add(new LiteralControl(BuildRow(RowCount++, row.properties)));
            }
            Controls.Add(new LiteralControl(BuildRow(RowCount++, new DataItemProperty[0])));
            string addRowClientEvent = Sitecore.Context.ClientPage.GetClientEvent(ID + ".AddRow");
            Controls.Add(new LiteralControl(string.Format(@"<div onclick=""{0}"" id=""{1}"">Add row</div>", addRowClientEvent, ID + "_add")));
        }

        protected string BuildRow(int rowIndex, IEnumerable<DataItemProperty> properties)
        {
            var template = GetPropertiesTemplate();
            if (!template.Any())
            {
                return string.Empty;
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("<table width=\"100%\" cellpadding=\"4\" cellspacing=\"0\" border=\"0\"><tr>");
            int width = 100 / template.Count();

            foreach (var pair in template)
            {
                string value = properties.Where(p => p.key == pair.Key).Select(p => p.value).FirstOrDefault() ?? string.Empty;
                string control = BuildPropertyControl(rowIndex, pair.Key, value, pair.Value);
                sb.AppendFormat("<td width=\"{1}%\">{0}</td>", control, width);
            }

            sb.Append("</tr></table>");

            return sb.ToString();
        }

        protected string BuildPropertyControl(int rowIndex, string key, string value, string type)
        {
            string uniqueId = GetUniqueID(ID + "_row_" + rowIndex.ToString() + "_");
            switch (type)
            {
                case "Image":
                    return BuildImageControl(uniqueId, key, value);
                case "Single - Line Text":
                    return BuildSingleLineControl(uniqueId, key, value);
                default:
                    return BuildSingleLineControl(uniqueId, key, value);
            }
            //TODO
        }

        protected string BuildImageControl(string id, string key, string value)
        {
            //TODO
            return BuildSingleLineControl(id, key, value);
        }

        protected string BuildSingleLineControl(string id, string key, string value)
        {
            using (HtmlTextWriter writer = new HtmlTextWriter(new StringWriter()))
            {
                writer.Write(@"<input name=""{0}"" type=""hidden"" value=""{1}"">", id, key);
                writer.Write(@"<input name=""{0}"" type=""text""{1}{2} style=""width:100%"" value=""{3}"">",
                    id + "_value",
                    ReadOnly ? " readonly=\"readonly\"" : string.Empty,
                    Disabled ? " disabled=\"disabled\"" : string.Empty,
                    HttpUtility.HtmlAttributeEncode(value));
                return writer.InnerWriter.ToString();
            }
        }

        protected Dictionary<string, string> GetPropertiesTemplate()
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            result.Add("image", "Image");
            result.Add("title", "Single - Line Text");
            result.Add("blurb", "Single - Line Text");

            //Rich Text
            //Multi - Line Text
            //Single - Line Text
            //Image with Cropping
            //Image

            //var i = GetItem();
            //i.Template.Fields[0].Type
            //TODO
            return result;
        }

        protected override void DoRender(HtmlTextWriter output)
        {
            Assert.ArgumentNotNull(output, "output");
            SetWidthAndHeightStyle();
            output.Write("<div" + ControlAttributes + ">");
            RenderChildren(output);
            output.Write("</div>");
        }

        [UsedImplicitly]
        protected void AddRow()
        {
            Sitecore.Context.ClientPage.ClientResponse.Insert(ID + "_add", "beforeBegin", BuildRow(RowCount++, new DataItemProperty[0]));
            //TODO - insert in diffrent component ID
            System.Web.UI.Page handler = HttpContext.Current.Handler as System.Web.UI.Page;
            if (handler != null && handler.Request.Form != null)
            {
                Sitecore.Context.ClientPage.ClientResponse.SetReturnValue(true);
            }
        }

        public string Source
        {
            get
            {
                return GetViewStateString("Source");
            }
            set
            {
                //Assert.ArgumentNotNull(value, "value");
                SetViewStateString("Source", value);
            }
        }

        public int RowCount
        {
            get
            {
                return GetViewStateInt("RowCount");
            }
            set
            {
                SetViewStateInt("RowCount", value);
            }
        }


        private DataValue _valueObject;
        public DataValue ValueObject
        {
            get
            {
                if (_valueObject == null)
                {
                    _valueObject = Value.JsonDeserialize<DataValue>() ?? new DataValue();
                }
                return _valueObject;
            }
            set
            {
                _valueObject = value;
                Value = _valueObject.JsonSerialize();
            }
        }

        [DataContract]
        public class DataValue
        {
            [DataMember]
            public List<DataItem> items = new List<DataItem>();
        }

        [DataContract]
        public class DataItem
        {
            [IgnoreDataMember]
            public int row;
            [DataMember]
            public List<DataItemProperty> properties = new List<DataItemProperty>();
        }

        [DataContract]
        public class DataItemProperty
        {
            [DataMember]
            public string key;
            [DataMember]
            public string value;
        }
    }
}

