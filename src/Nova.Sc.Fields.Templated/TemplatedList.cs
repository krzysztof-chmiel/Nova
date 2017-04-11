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
using Sitecore.Data.Managers;
using Nova.Core.Utils;
using Nova.Sc.Fields.Templated.Table;

namespace Nova.Sc.Fields.Templated
{
    [UsedImplicitly]
    public class TemplatedList : Input
    {
        const string NON_DATA_KEY = "non-data";

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
                foreach(var row in Controls.Filter<Table.Table>().SelectMany(t => t.Rows).Where(r => r.Key != NON_DATA_KEY))
                {
                    DataItem dataItem = new DataItem();

                    foreach(var cell in row.Cells.Where(c => c.Key != NON_DATA_KEY))
                    {
                        dataItem.properties.Add(new DataItemProperty() { key = cell.Key, value = GetCellValue(cell) });
                    }
                    valueObject.items.Add(dataItem);
                }
                //TODO - order
                valueObject.items = valueObject.items.Where(i => i.properties.Any(p => !string.IsNullOrEmpty(p.value))).ToList();
                string serializedValue = valueObject.items.Any() ? valueObject.JsonSerialize() : string.Empty;

                if (Value != serializedValue)
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

            foreach (var row in ValueObject.items)
            {
                Controls.Add(GetDataItemControl(row));
            }
            Controls.Add(GetDataItemControl(null));
        }

        [UsedImplicitly]
        protected void RowAddUp(string tableId)
        {
            var nav = new ObjectNavigation<Table.Table, string>(Controls.Filter<Table.Table>(), tableId, t => t.ID);

            if (nav.Current != null)
            {
                Table.Table table = GetDataItemControl(null);
                Controls.AddAt(Controls.IndexOf(nav.Current), table);
                Sitecore.Context.ClientPage.ClientResponse.Insert(tableId, "beforeBegin", table);
            }

            System.Web.UI.Page handler = HttpContext.Current.Handler as System.Web.UI.Page;
            if (handler != null && handler.Request.Form != null)
            {
                Sitecore.Context.ClientPage.ClientResponse.SetReturnValue(true);
            }
        }

        [UsedImplicitly]
        protected void RowAddDown(string tableId)
        {
            var nav = new ObjectNavigation<Table.Table, string>(Controls.Filter<Table.Table>(), tableId, t => t.ID);

            if (nav.Current != null)
            {
                Table.Table table = GetDataItemControl(null);
                Controls.AddAt(Controls.IndexOf(nav.Current)+1, table);
                Sitecore.Context.ClientPage.ClientResponse.Insert(tableId, "afterEnd", table);
            }

            System.Web.UI.Page handler = HttpContext.Current.Handler as System.Web.UI.Page;
            if (handler != null && handler.Request.Form != null)
            {
                Sitecore.Context.ClientPage.ClientResponse.SetReturnValue(true);
            }
        }


        [UsedImplicitly]
        protected void RowUp(string tableId)
        {
            var nav = new ObjectNavigation<Table.Table, string>(Controls.Filter<Table.Table>(), tableId, t => t.ID);

            if(nav.Previous != null && nav.Current != null)
            {
                SwitchValues(nav.Previous, nav.Current);
                Sitecore.Context.ClientPage.ClientResponse.Refresh(nav.Previous);
                Sitecore.Context.ClientPage.ClientResponse.Refresh(nav.Current);
            }

            System.Web.UI.Page handler = HttpContext.Current.Handler as System.Web.UI.Page;
            if (handler != null && handler.Request.Form != null)
            {
                Sitecore.Context.ClientPage.ClientResponse.SetReturnValue(true);
            }
        }

        [UsedImplicitly]
        protected void RowDown(string tableId)
        {
            var nav = new ObjectNavigation<Table.Table, string>(Controls.Filter<Table.Table>(), tableId, t => t.ID);

            if (nav.Next != null && nav.Current != null)
            {
                SwitchValues(nav.Next, nav.Current);
                Sitecore.Context.ClientPage.ClientResponse.Refresh(nav.Next);
                Sitecore.Context.ClientPage.ClientResponse.Refresh(nav.Current);
            }

            System.Web.UI.Page handler = HttpContext.Current.Handler as System.Web.UI.Page;
            if (handler != null && handler.Request.Form != null)
            {
                Sitecore.Context.ClientPage.ClientResponse.SetReturnValue(true);
            }
        }

        [UsedImplicitly]
        protected void RowDelete(string tableId)
        {
            Table.Table table = Controls.Filter<Table.Table>().FirstOrDefault(t => t.ID == tableId);

            if(table != null)
            {
                Controls.Remove(table);
                Sitecore.Context.ClientPage.ClientResponse.Remove(tableId);
            }

            System.Web.UI.Page handler = HttpContext.Current.Handler as System.Web.UI.Page;
            if (handler != null && handler.Request.Form != null)
            {
                Sitecore.Context.ClientPage.ClientResponse.SetReturnValue(true);
            }
        }

        [UsedImplicitly]
        protected void RowClear(string tableId)
        {
            Table.Table table = Controls.Filter<Table.Table>().FirstOrDefault(t => t.ID == tableId);

            if (table != null)
            {
                Table.Table defaultTable = GetDataItemControl(null);
                CopyValues(defaultTable, table);
                Sitecore.Context.ClientPage.ClientResponse.Refresh(table);
            }

            System.Web.UI.Page handler = HttpContext.Current.Handler as System.Web.UI.Page;
            if (handler != null && handler.Request.Form != null)
            {
                Sitecore.Context.ClientPage.ClientResponse.SetReturnValue(true);
            }
        }

        [UsedImplicitly]
        protected void RowClone(string tableId)
        {
            var nav = new ObjectNavigation<Table.Table, string>(Controls.Filter<Table.Table>(), tableId, t => t.ID);

            if (nav.Current != null)
            {
                Table.Table table = GetDataItemControl(null);
                CopyValues(nav.Current, table);
                Controls.AddAt(Controls.IndexOf(nav.Current) + 1, table);
                Sitecore.Context.ClientPage.ClientResponse.Insert(tableId, "afterEnd", table);
            }

            System.Web.UI.Page handler = HttpContext.Current.Handler as System.Web.UI.Page;
            if (handler != null && handler.Request.Form != null)
            {
                Sitecore.Context.ClientPage.ClientResponse.SetReturnValue(true);
            }
        }

        [UsedImplicitly]
        protected void RowFirst(string tableId)
        {
            var nav = new ObjectNavigation<Table.Table, string>(Controls.Filter<Table.Table>(), tableId, t => t.ID);

            if(nav.Current != null && nav.Current != nav.First)
            {
                Controls.Remove(nav.Current);
                Controls.AddAt(0, nav.Current);
                Sitecore.Context.ClientPage.ClientResponse.Remove(tableId);
                Sitecore.Context.ClientPage.ClientResponse.Insert(nav.First.ID, "beforeBegin", nav.Current);
            }

            System.Web.UI.Page handler = HttpContext.Current.Handler as System.Web.UI.Page;
            if (handler != null && handler.Request.Form != null)
            {
                Sitecore.Context.ClientPage.ClientResponse.SetReturnValue(true);
            }
        }

        [UsedImplicitly]
        protected void RowLast(string tableId)
        {
            var nav = new ObjectNavigation<Table.Table, string>(Controls.Filter<Table.Table>(), tableId, t => t.ID);

            if (nav.Current != null && nav.Current != nav.Last)
            {
                Controls.Remove(nav.Current);
                Controls.Add(nav.Current);
                Sitecore.Context.ClientPage.ClientResponse.Remove(tableId);
                Sitecore.Context.ClientPage.ClientResponse.Insert(nav.Last.ID, "afterEnd", nav.Current);
            }

            System.Web.UI.Page handler = HttpContext.Current.Handler as System.Web.UI.Page;
            if (handler != null && handler.Request.Form != null)
            {
                Sitecore.Context.ClientPage.ClientResponse.SetReturnValue(true);
            }
        }

        protected void SwitchValues(Table.Table source, Table.Table target)
        {
            var sourceCells = source.Rows.Where(r => r.Key != NON_DATA_KEY).SelectMany(r => r.Cells).Where(c => c.Key != NON_DATA_KEY).ToArray();
            var targetCells = target.Rows.Where(r => r.Key != NON_DATA_KEY).SelectMany(r => r.Cells).Where(c => c.Key != NON_DATA_KEY).ToArray();
            for(int i = 0; i<sourceCells.Length; i++)
            {
                var sourceValue = GetCellValue(sourceCells[i]);
                var targetValue = GetCellValue(targetCells[i]);

                SetValue(sourceCells[i], targetValue);
                SetValue(targetCells[i], sourceValue);
            }
        }

        protected void CopyValues(Table.Table source, Table.Table target)
        {
            var sourceCells = source.Rows.Where(r => r.Key != NON_DATA_KEY).SelectMany(r => r.Cells).Where(c => c.Key != NON_DATA_KEY).ToArray();
            var targetCells = target.Rows.Where(r => r.Key != NON_DATA_KEY).SelectMany(r => r.Cells).Where(c => c.Key != NON_DATA_KEY).ToArray();
            for (int i = 0; i < sourceCells.Length; i++)
            {
                var sourceValue = GetCellValue(sourceCells[i]);
                SetValue(targetCells[i], sourceValue);
            }
        }

        protected Table.Table GetDataItemControl(DataItem dataItem)
        {
            Table.Table result = new Table.Table() { ID = GetUniqueID(ID) };

            Table.Row headerRow = new Table.Row() { Key = NON_DATA_KEY };
            Table.Row menuRow = new Table.Row() { Key = NON_DATA_KEY };
            Table.Row tableRow = new Table.Row();

            headerRow.AddCell(new Table.Cell());
            menuRow.AddCell(new Table.Cell());
            tableRow.AddCell(GetNavigationCell(result.ID));

            foreach (var field in SourceRowTemplate)
            {
                string editorId = GetUniqueID(ID);
                Item fieldItemType = GetFieldTypeItem(field);

                headerRow.AddCell(new Table.Cell(new LiteralWithViewState(HttpUtility.HtmlEncode(field.DisplayName ?? field.Name)), NON_DATA_KEY));
                menuRow.AddCell(new Table.Cell(GetMenuButtons(fieldItemType, editorId), NON_DATA_KEY));


                System.Web.UI.Control editor = GetEditor(fieldItemType);
                SetProperties(editor, editorId, field);
                //TODO - Sitecore.Shell.Applications.ContentEditor.EditorFormatter.SetAttributes
                //TODO - Sitecore.Shell.Applications.ContentEditor.EditorFormatter.SetStyle
                if (dataItem != null)
                {
                    SetValue(editor, dataItem.properties.Where(p => p.key == field.Name).Select(p => p.value).FirstOrDefault());
                }
                tableRow.AddCell(new Table.Cell(editor, field.Name));
            }

            result.AddRow(headerRow);
            result.AddRow(menuRow);
            result.AddRow(tableRow);

            return result;
        }

        protected Table.Cell GetNavigationCell(string tableId)
        {
            string firstClientEvent = Sitecore.Context.ClientPage.GetClientEvent(string.Format("{0}.RowFirst(\"{1}\")", ID, tableId));
            string upClientEvent = Sitecore.Context.ClientPage.GetClientEvent(string.Format("{0}.RowUp(\"{1}\")", ID, tableId));
            string addUpClientEvent = Sitecore.Context.ClientPage.GetClientEvent(string.Format("{0}.RowAddUp(\"{1}\")", ID, tableId));

            string deleteClientEvent = Sitecore.Context.ClientPage.GetClientEvent(string.Format("{0}.RowDelete(\"{1}\")", ID, tableId));
            string clearClientEvent = Sitecore.Context.ClientPage.GetClientEvent(string.Format("{0}.RowClear(\"{1}\")", ID, tableId));
            string cloneClientEvent = Sitecore.Context.ClientPage.GetClientEvent(string.Format("{0}.RowClone(\"{1}\")", ID, tableId));

            string lastClientEvent = Sitecore.Context.ClientPage.GetClientEvent(string.Format("{0}.RowLast(\"{1}\")", ID, tableId));
            string downClientEvent = Sitecore.Context.ClientPage.GetClientEvent(string.Format("{0}.RowDown(\"{1}\")", ID, tableId));
            string addDownClientEvent = Sitecore.Context.ClientPage.GetClientEvent(string.Format("{0}.RowAddDown(\"{1}\")", ID, tableId));

            var sb = new StringBuilder();
            sb.AppendFormat(@"<div id=""{0}"" class=""tlNavigation"">", tableId + "_nav");
            sb.AppendLine("<div>");
            sb.AppendFormat(@"<span onclick=""{0}"" id=""{1}"" class=""tlFirst"">{2}</span>", firstClientEvent, tableId + "_first", ThemeManager.GetImage("office/16x16/navigate_up2.png", 16, 16));
            sb.AppendFormat(@"<span onclick=""{0}"" id=""{1}"" class=""tlUp"">{2}</span>", upClientEvent, tableId + "_up", ThemeManager.GetImage("office/16x16/navigate_up.png", 16, 16));
            sb.AppendFormat(@"<span onclick=""{0}"" id=""{1}"" class=""tlAddUp"">{2}</span>", addUpClientEvent, tableId + "_addUp", ThemeManager.GetImage("office/16x16/navigate_plus.png", 16, 16));
            sb.AppendLine(" </div>");
            sb.AppendLine("<div>");
            sb.AppendFormat(@"<span onclick=""{0}"" id=""{1}"" class=""tlDelete"">{2}</span>", deleteClientEvent, tableId + "_delete", ThemeManager.GetImage("office/16x16/delete.png", 16, 16));
            sb.AppendFormat(@"<span onclick=""{0}"" id=""{1}"" class=""tlClear"">{2}</span>", clearClientEvent, tableId + "_clear", ThemeManager.GetImage("office/16x16/selection.png", 16, 16));
            sb.AppendFormat(@"<span onclick=""{0}"" id=""{1}"" class=""tlClone"">{2}</span>", cloneClientEvent, tableId + "_clone", ThemeManager.GetImage("office/16x16/arrow_fork.png", 16, 16));
            sb.AppendLine(" </div>");
            sb.AppendLine("<div>");
            sb.AppendFormat(@"<span onclick=""{0}"" id=""{1}"" class=""tlLast"">{2}</span>", lastClientEvent, tableId + "_last", ThemeManager.GetImage("office/16x16/navigate_down2.png", 16, 16));
            sb.AppendFormat(@"<span onclick=""{0}"" id=""{1}"" class=""tlDown"">{2}</span>", downClientEvent, tableId + "_down", ThemeManager.GetImage("office/16x16/navigate_down.png", 16, 16));
            sb.AppendFormat(@"<span onclick=""{0}"" id=""{1}"" class=""tlAddDown"">{2}</span>", addDownClientEvent, tableId + "_addDown", ThemeManager.GetImage("office/16x16/navigate_plus.png", 16, 16));
            sb.AppendLine(" </div>");
            sb.AppendLine(" </div>");

            return new Table.Cell(new LiteralWithViewState(sb.ToString()), NON_DATA_KEY) { CssClass = "tlNavigation" };
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
                    return new LiteralWithViewState(writer.InnerWriter.ToString());
                }
            }
            return new LiteralWithViewState();
        }

        public void SetValue(System.Web.UI.Control editor, string value)
        {
            if(editor as Table.Cell != null)
            {
                editor = editor.Controls.Filter<System.Web.UI.Control>().FirstOrDefault() ?? editor;
            }

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
                Value = _valueObject.items.Any() ? _valueObject.JsonSerialize() : string.Empty;
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

