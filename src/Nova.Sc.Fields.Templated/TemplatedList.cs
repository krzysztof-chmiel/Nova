using Sitecore;
using Sitecore.Diagnostics;
using Sitecore.Web.UI.HtmlControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.UI;
using Nova.Core;
using System.Web;
using System.IO;
using Sitecore.Shell.Applications.ContentEditor;
using Sitecore.Resources;
using Sitecore.Reflection;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Nova.Core.Utils;
using Nova.Web.UI;

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


        private Value.List _valueObject;
        public Value.List ValueObject
        {
            get
            {
                if (_valueObject == null)
                {
                    _valueObject = Value.JsonDeserialize<Value.List>() ?? new Value.List();
                }
                return _valueObject;
            }
            set
            {
                _valueObject = value;
                Value = _valueObject.items.Any() ? _valueObject.JsonSerialize() : string.Empty;
            }
        }

        private List<Item> _sourceRowTemplate;
        protected List<Item> SourceRowTemplate
        {
            get
            {
                if (_sourceRowTemplate == null)
                {
                    _sourceRowTemplate = new List<Item>();
                    var source = Sitecore.Context.ContentDatabase.GetItem(Source);
                    if (source != null)
                    {
                        foreach (Item section in source.Children)
                        {
                            _sourceRowTemplate.AddRange(section.Children);
                        }
                    }
                }
                return _sourceRowTemplate;
            }
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
                Value.List valueObject = new Value.List();
                foreach(var row in Controls.Filter<Table>().SelectMany(t => t.Rows).Where(r => r.Key != NON_DATA_KEY))
                {
                    Value.Item valueItem = new Value.Item();

                    foreach(var cell in row.Cells.Where(c => c.Key != NON_DATA_KEY))
                    {
                        valueItem.properties.Add(new Value.Property() { key = cell.Key, value = SitecoreReflected.Instance.GetValue(cell) });
                    }
                    valueObject.items.Add(valueItem);
                }

                valueObject.items = valueObject.items.Where(i => i.properties.Any(p => !string.IsNullOrEmpty(p.value))).ToList();
                string serializedValue = valueObject.items.Any() ? valueObject.JsonSerialize() : string.Empty;

                if (Value != serializedValue)
                {
                    ValueObject = valueObject;
                    SetModified();
                }
            }
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

        protected void ClientResponseReturnValue(bool value)
        {
            System.Web.UI.Page handler = HttpContext.Current.Handler as System.Web.UI.Page;
            if (handler != null && handler.Request.Form != null)
            {
                Sitecore.Context.ClientPage.ClientResponse.SetReturnValue(value);
            }
        }

        [UsedImplicitly]
        protected void RowAddUp(string tableId)
        {
            var nav = new EnumerableNavigation<Table, string>(Controls.Filter<Table>(), tableId, t => t.ID);

            if (nav.Current != null)
            {
                Table table = GetDataItemControl(null);
                Controls.AddAt(Controls.IndexOf(nav.Current), table);
                Sitecore.Context.ClientPage.ClientResponse.Insert(tableId, "beforeBegin", table);
            }

            ClientResponseReturnValue(true);
        }

        [UsedImplicitly]
        protected void RowAddDown(string tableId)
        {
            var nav = new EnumerableNavigation<Table, string>(Controls.Filter<Table>(), tableId, t => t.ID);

            if (nav.Current != null)
            {
                Table table = GetDataItemControl(null);
                Controls.AddAt(Controls.IndexOf(nav.Current)+1, table);
                Sitecore.Context.ClientPage.ClientResponse.Insert(tableId, "afterEnd", table);
            }

            ClientResponseReturnValue(true);
        }

        [UsedImplicitly]
        protected void RowUp(string tableId)
        {
            var nav = new EnumerableNavigation<Table, string>(Controls.Filter<Table>(), tableId, t => t.ID);

            if(nav.Previous != null && nav.Current != null)
            {
                SwitchValues(nav.Previous, nav.Current);
                Sitecore.Context.ClientPage.ClientResponse.Refresh(nav.Previous);
                Sitecore.Context.ClientPage.ClientResponse.Refresh(nav.Current);
            }

            ClientResponseReturnValue(true);
        }

        [UsedImplicitly]
        protected void RowDown(string tableId)
        {
            var nav = new EnumerableNavigation<Table, string>(Controls.Filter<Table>(), tableId, t => t.ID);

            if (nav.Next != null && nav.Current != null)
            {
                SwitchValues(nav.Next, nav.Current);
                Sitecore.Context.ClientPage.ClientResponse.Refresh(nav.Next);
                Sitecore.Context.ClientPage.ClientResponse.Refresh(nav.Current);
            }

            ClientResponseReturnValue(true);
        }

        [UsedImplicitly]
        protected void RowDelete(string tableId)
        {
            Table table = Controls.Filter<Table>().FirstOrDefault(t => t.ID == tableId);

            if(table != null)
            {
                Controls.Remove(table);
                Sitecore.Context.ClientPage.ClientResponse.Remove(tableId);
            }

            ClientResponseReturnValue(true);
        }

        [UsedImplicitly]
        protected void RowClear(string tableId)
        {
            Table table = Controls.Filter<Table>().FirstOrDefault(t => t.ID == tableId);

            if (table != null)
            {
                Table defaultTable = GetDataItemControl(null);
                CopyValues(defaultTable, table);
                Sitecore.Context.ClientPage.ClientResponse.Refresh(table);
            }

            ClientResponseReturnValue(true);
        }

        [UsedImplicitly]
        protected void RowClone(string tableId)
        {
            var nav = new EnumerableNavigation<Table, string>(Controls.Filter<Table>(), tableId, t => t.ID);

            if (nav.Current != null)
            {
                Table table = GetDataItemControl(null);
                CopyValues(nav.Current, table);
                Controls.AddAt(Controls.IndexOf(nav.Current) + 1, table);
                Sitecore.Context.ClientPage.ClientResponse.Insert(tableId, "afterEnd", table);
            }

            ClientResponseReturnValue(true);
        }

        [UsedImplicitly]
        protected void RowFirst(string tableId)
        {
            var nav = new EnumerableNavigation<Table, string>(Controls.Filter<Table>(), tableId, t => t.ID);

            if(nav.Current != null && nav.Current != nav.First)
            {
                Controls.Remove(nav.Current);
                Controls.AddAt(0, nav.Current);
                Sitecore.Context.ClientPage.ClientResponse.Remove(tableId);
                Sitecore.Context.ClientPage.ClientResponse.Insert(nav.First.ID, "beforeBegin", nav.Current);
            }

            ClientResponseReturnValue(true);
        }

        [UsedImplicitly]
        protected void RowLast(string tableId)
        {
            var nav = new EnumerableNavigation<Table, string>(Controls.Filter<Table>(), tableId, t => t.ID);

            if (nav.Current != null && nav.Current != nav.Last)
            {
                Controls.Remove(nav.Current);
                Controls.Add(nav.Current);
                Sitecore.Context.ClientPage.ClientResponse.Remove(tableId);
                Sitecore.Context.ClientPage.ClientResponse.Insert(nav.Last.ID, "afterEnd", nav.Current);
            }

            ClientResponseReturnValue(true);
        }

        protected void SwitchValues(Table source, Table target)
        {
            var sourceCells = source.Rows.Where(r => r.Key != NON_DATA_KEY).SelectMany(r => r.Cells).Where(c => c.Key != NON_DATA_KEY).ToArray();
            var targetCells = target.Rows.Where(r => r.Key != NON_DATA_KEY).SelectMany(r => r.Cells).Where(c => c.Key != NON_DATA_KEY).ToArray();
            for(int i = 0; i<sourceCells.Length; i++)
            {
                var sourceValue = SitecoreReflected.Instance.GetValue(sourceCells[i]);
                var targetValue = SitecoreReflected.Instance.GetValue(targetCells[i]);

                SitecoreReflected.Instance.SetValue(sourceCells[i], targetValue);
                SitecoreReflected.Instance.SetValue(targetCells[i], sourceValue);
            }
        }

        protected void CopyValues(Table source, Table target)
        {
            var sourceCells = source.Rows.Where(r => r.Key != NON_DATA_KEY).SelectMany(r => r.Cells).Where(c => c.Key != NON_DATA_KEY).ToArray();
            var targetCells = target.Rows.Where(r => r.Key != NON_DATA_KEY).SelectMany(r => r.Cells).Where(c => c.Key != NON_DATA_KEY).ToArray();
            for (int i = 0; i < sourceCells.Length; i++)
            {
                var sourceValue = SitecoreReflected.Instance.GetValue(sourceCells[i]);
                SitecoreReflected.Instance.SetValue(targetCells[i], sourceValue);
            }
        }

        protected Table GetDataItemControl(Value.Item valueItem)
        {
            Table result = new Table() { ID = GetUniqueID(ID) };

            Row headerRow = new Row() { Key = NON_DATA_KEY };
            Row menuRow = new Row() { Key = NON_DATA_KEY };
            Row tableRow = new Row();

            headerRow.AddCell(new Cell());
            menuRow.AddCell(new Cell());
            tableRow.AddCell(GetNavigationCell(result.ID));

            foreach (var field in SourceRowTemplate)
            {
                string editorId = GetUniqueID(ID);
                Item fieldItemType = SitecoreReflected.Instance.GetFieldTypeItem(field);

                headerRow.AddCell(new Cell(new LiteralWithViewState(HttpUtility.HtmlEncode(field.DisplayName ?? field.Name)), NON_DATA_KEY));
                menuRow.AddCell(new Cell(SitecoreReflected.Instance.GetMenuButtons(fieldItemType, editorId), NON_DATA_KEY));


                System.Web.UI.Control editor = SitecoreReflected.Instance.GetEditor(fieldItemType);
                SitecoreReflected.Instance.SetProperties(editor, editorId, field, ItemID, ItemVersion, ItemLanguage, ReadOnly);
                //TODO - Sitecore.Shell.Applications.ContentEditor.EditorFormatter.SetAttributes
                //TODO - Sitecore.Shell.Applications.ContentEditor.EditorFormatter.SetStyle
                if (valueItem != null)
                {
                    SitecoreReflected.Instance.SetValue(editor, valueItem.properties.Where(p => p.key == field.Name).Select(p => p.value).FirstOrDefault());
                }
                tableRow.AddCell(new Cell(editor, field.Name));
            }

            result.AddRow(headerRow);
            result.AddRow(menuRow);
            result.AddRow(tableRow);

            return result;
        }

        protected Cell GetNavigationCell(string tableId)
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

            return new Cell(new LiteralWithViewState(sb.ToString()), NON_DATA_KEY) { CssClass = "tlNavigation" };
        }

        protected override void DoRender(HtmlTextWriter output)
        {
            Assert.ArgumentNotNull(output, "output");
            SetWidthAndHeightStyle();
            output.Write("<div" + ControlAttributes + ">");
            RenderChildren(output);
            output.Write("</div>");
        }
    }
}

