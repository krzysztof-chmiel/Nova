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
using Sitecore.Resources;
using Sitecore.Reflection;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Shell.Framework.Commands;

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
                foreach (Table.Table table in Controls)
                {
                    foreach(var row in table.Rows.Where(r => r.Key != "non-data"))
                    {
                        DataItem dataItem = new DataItem();

                        foreach(var cell in row.Cells)
                        {
                            dataItem.properties.Add(new DataItemProperty() { key = cell.Key, value = GetCellValue(cell) });
                        }
                        valueObject.items.Add(dataItem);
                    }
                }
                //TODO - order
                valueObject.items = valueObject.items.Where(i => i.properties.Any(p => !string.IsNullOrEmpty(p.value))).ToList();
                if (Value != valueObject.JsonSerialize())
                {
                    ValueObject = valueObject;
                    SetModified();
                }
            }
        }

        private string GetCellValue(Table.Cell cell)
        {
            foreach(System.Web.UI.Control c in cell.Controls)
            {
                return GetValue(c);
            }
            return null;
        }

        private void BuildControl()
        {
            if (!SourceRowTemplate.Any())
            {
                return;
            }

            Table.Table table = new Table.Table();
            Table.Row header = new Table.Row() { Key = "non-data" };
            Table.Row newRow = new Table.Row();
            Table.Row newRowMenu = new Table.Row() { Key = "non-data" };
            table.AddRow(header);
            foreach(var field in SourceRowTemplate)
            {
                string editorId = GetUniqueID(ID);
                Item fieldItemType = GetFieldTypeItem(field);

                header.AddCell(new Table.Cell(new LiteralControl(HttpUtility.HtmlEncode(field.DisplayName ?? field.Name)), "header"));

                System.Web.UI.Control editor = GetEditor(fieldItemType);
                SetProperties(editor, editorId, field);
                //TODO - Sitecore.Shell.Applications.ContentEditor.EditorFormatter.SetAttributes
                //TODO - Sitecore.Shell.Applications.ContentEditor.EditorFormatter.SetStyle
                
                newRow.AddCell(new Table.Cell(editor, field.Name));
                newRowMenu.AddCell(new Table.Cell(GetMenuButtons(fieldItemType, editorId), "non-data"));
            }

            foreach (var row in ValueObject.items)
            {
                Table.Row tableRow = new Table.Row();
                Table.Row menuRow = new Table.Row() { Key = "non-data" };
                foreach (var field in SourceRowTemplate)
                {
                    string editorId = GetUniqueID(ID);
                    Item fieldItemType = GetFieldTypeItem(field);
                    System.Web.UI.Control editor = GetEditor(fieldItemType);
                    SetProperties(editor, editorId, field);
                    //TODO - Sitecore.Shell.Applications.ContentEditor.EditorFormatter.SetAttributes
                    //TODO - Sitecore.Shell.Applications.ContentEditor.EditorFormatter.SetStyle
                    SetValue(editor, row.properties.Where(p => p.key == field.Name).Select(p => p.value).FirstOrDefault());
                    tableRow.AddCell(new Table.Cell(editor, field.Name));

                    menuRow.AddCell(new Table.Cell(GetMenuButtons(fieldItemType, editorId), "non-data"));
                }

                table.AddRow(menuRow);
                table.AddRow(tableRow);
            }

            table.AddRow(newRowMenu);
            table.AddRow(newRow);
            Controls.Add(table);
        }

        private List<Item> _sourceRowTemplate;
        protected List<Item> SourceRowTemplate
        {
            get
            {
                if(_sourceRowTemplate == null)
                {
                    _sourceRowTemplate = new List<Item>();
                    var source = Sitecore.Context.ContentDatabase.GetItem(Source);
                    if(source != null)
                    {
                        foreach(Item section in source.Children)
                        {
                            _sourceRowTemplate.AddRange(section.Children);
                        }
                    }
                }
                return _sourceRowTemplate;
            }
        }

        protected override void DoRender(HtmlTextWriter output)
        {
            Assert.ArgumentNotNull(output, "output");
            SetWidthAndHeightStyle();
            output.Write("<div" + ControlAttributes + ">");
            RenderChildren(output);
            output.Write("</div>");
        }

        public Item GetFieldTypeItem(Item field)
        {
            string fieldType = field["Type"];
            if (string.IsNullOrEmpty(fieldType))
            {
                fieldType = "text";
            }

            return FieldTypeManager.GetFieldTypeItem(fieldType) ?? FieldTypeManager.GetDefaultFieldTypeItem();
        }

        public System.Web.UI.Control GetEditor(Item fieldTypeItem)
        {
            System.Web.UI.Control control = Resource.GetWebControl(fieldTypeItem["Control"]);
            if (control == null)
            {
                string text = fieldTypeItem["Assembly"];
                string text2 = fieldTypeItem["Class"];
                if (!string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(text2))
                {
                    control = (ReflectionUtil.CreateObject(text, text2, new object[0]) as System.Web.UI.Control);
                }
            }
            if (control == null)
            {
                control = new Text();
            }
            return control;
        }

        public void SetProperties(System.Web.UI.Control editor, string editorId, Item field) //string fieldId, string fieldSource)
        {
            ReflectionUtil.SetProperty(editor, "ID", editorId); //field.ControlID
            ReflectionUtil.SetProperty(editor, "ItemID", ItemID);
            ReflectionUtil.SetProperty(editor, "ItemVersion", ItemVersion);
            ReflectionUtil.SetProperty(editor, "ItemLanguage", ItemLanguage);
            ReflectionUtil.SetProperty(editor, "FieldID", field.ID.ToString()); //field.ItemField.ID.ToString()
            ReflectionUtil.SetProperty(editor, "Source", field["Source"]); //field.ItemField.Source
            ReflectionUtil.SetProperty(editor, "ReadOnly", ReadOnly);
            ReflectionUtil.SetProperty(editor, "Disabled", ReadOnly);
        }

        public System.Web.UI.Control GetMenuButtons(Item fieldTypeItem, string editorId)
        {
            Item menu = fieldTypeItem.Children["Menu"];
            if (menu != null && menu.HasChildren)
            {
                using (HtmlTextWriter writer = new HtmlTextWriter(new StringWriter()))
                {
                    writer.Write("<div class=\"scContentButtons\">");
                    foreach(Item item in menu.Children)
                    {
                        if(MainUtil.GetBool(item["Show In Field Editor"], false))
                        {
                            writer.Write(string.Format("<a href=\"#\" class=\"scContentButton\" onclick=\"{0}\">",
                                Sitecore.Context.ClientPage.GetClientEvent(item["Message"]).Replace("$Target", editorId)));
                            writer.Write(item["Display Name"]);
                            writer.Write("</a>");
                        }
                    }
                    writer.Write("</div>");
                    return new LiteralControl(writer.InnerWriter.ToString());
                }
            }
            return new LiteralControl();
        }

        public void SetValue(System.Web.UI.Control editor, string value)
        {
            value = value ?? string.Empty;

            if (editor is IStreamedContentField)
            {
                return;
            }
            IContentField contentField = editor as IContentField;
            if (contentField != null)
            {
                contentField.SetValue(value);
                return;
            }
            ReflectionUtil.SetProperty(editor, "Value", value);
        }

        public string GetValue(System.Web.UI.Control editor)
        {
            IContentField contentField = editor as IContentField;
            if (contentField != null)
            {
                return contentField.GetValue();
            }

            object value = ReflectionUtil.GetProperty(editor, "Value");
            return value == null ? null : value.ToString();
        }

        public string Source
        {
            get
            {
                return GetViewStateString("Source");
            }
            set
            {
                SetViewStateString("Source", value);
            }
        }

        public string ItemID
        {
            get
            {
                return GetViewStateString("ItemID");
            }
            set
            {
                SetViewStateString("ItemID", value);
            }
        }

        public string ItemVersion
        {
            get
            {
                return GetViewStateString("ItemVersion");
            }
            set
            {
                SetViewStateString("ItemVersion", value);
            }
        }

        public string ItemLanguage
        {
            get
            {
                return GetViewStateString("ItemLanguage");
            }
            set
            {
                SetViewStateString("ItemLanguage", value);
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

