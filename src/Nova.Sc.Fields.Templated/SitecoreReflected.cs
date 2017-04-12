using Nova.Web.UI;
using Sitecore;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Reflection;
using Sitecore.Resources;
using Sitecore.Shell.Applications.ContentEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI;
using Nova.Core;

namespace Nova.Sc.Fields.Templated
{
    public class SitecoreReflected
    {
        protected SitecoreReflected()
        {

        }

        protected static SitecoreReflected _instance;
        public static SitecoreReflected Instance
        {
            get
            {
                if(_instance == null)
                {
                    _instance = new SitecoreReflected();
                }
                return _instance;
            }
            set
            {
                _instance = value;
            }
        }

        public virtual Item GetFieldTypeItem(Item field)
        {
            string fieldType = field["Type"];
            if (string.IsNullOrEmpty(fieldType))
            {
                fieldType = "text";
            }

            return FieldTypeManager.GetFieldTypeItem(fieldType) ?? FieldTypeManager.GetDefaultFieldTypeItem();
        }

        public virtual System.Web.UI.Control GetEditor(Item fieldTypeItem)
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

        public virtual void SetProperties(System.Web.UI.Control editor, string editorId, Item field, string itemId, string itemVersion, string itemLanguage, bool readOnly) //string fieldId, string fieldSource)
        {
            ReflectionUtil.SetProperty(editor, "ID", editorId); //field.ControlID
            ReflectionUtil.SetProperty(editor, "ItemID", itemId);
            ReflectionUtil.SetProperty(editor, "ItemVersion", itemVersion);
            ReflectionUtil.SetProperty(editor, "ItemLanguage", itemLanguage);
            ReflectionUtil.SetProperty(editor, "FieldID", field.ID.ToString()); //field.ItemField.ID.ToString()
            ReflectionUtil.SetProperty(editor, "Source", field["Source"]); //field.ItemField.Source
            ReflectionUtil.SetProperty(editor, "ReadOnly", readOnly);
            ReflectionUtil.SetProperty(editor, "Disabled", readOnly);
        }

        public virtual System.Web.UI.Control GetMenuButtons(Item fieldTypeItem, string editorId)
        {
            Item menu = fieldTypeItem.Children["Menu"];
            if (menu != null && menu.HasChildren)
            {
                using (HtmlTextWriter writer = new HtmlTextWriter(new StringWriter()))
                {
                    writer.Write("<div class=\"scContentButtons\">");
                    foreach (Item item in menu.Children)
                    {
                        if (MainUtil.GetBool(item["Show In Field Editor"], false))
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

        public virtual void SetValue(System.Web.UI.Control editor, string value)
        {
            if (editor as Cell != null)
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

        public virtual string GetValue(System.Web.UI.Control editor)
        {
            if (editor as Cell != null)
            {
                editor = editor.Controls.Filter<System.Web.UI.Control>().FirstOrDefault() ?? editor;
            }

            IContentField contentField = editor as IContentField;
            if (contentField != null)
            {
                return contentField.GetValue();
            }

            object value = ReflectionUtil.GetProperty(editor, "Value");
            return value == null ? null : value.ToString();
        }
    }
}
