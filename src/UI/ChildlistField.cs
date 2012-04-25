using Sitecore.Data;

namespace SitecoreExtension.ChildlistField.UI
{
    using System.Collections.Generic;
    using System.Text;
    using System.Web.UI;

    using Sitecore;
    using Sitecore.Data.Items;
    using Sitecore.Diagnostics;
    using Sitecore.Globalization;
    using Sitecore.Resources;
    using Sitecore.Shell.Applications.ContentEditor;
    using Sitecore.Web.UI.HtmlControls;
    using Sitecore.Web.UI.Sheer;

    /// <summary>
    /// Context field (used in the Content editor)
    /// </summary>
    public class ChildlistField : Custom
    {
        #region Fields

        List<Item> _children = null;
        private string _selectedId = null;

        #endregion Fields

        #region Properties

        /// <summary>
        /// Gets or sets the children.
        /// </summary>
        /// <value>The children.</value>
        public List<Item> Children
        {
            get
            {
                if (_children == null)
                {
                    _children = GetChildren();
                }
                return _children;
            }
            set
            {
                _children = value;
            }
        }

        /// <summary>
        /// Gets or sets the selected id.
        /// </summary>
        /// <value>The selected id.</value>
        public string SelectedId
        {
            get
            {
                if(_selectedId == null)
                {
                    _selectedId = Sitecore.Context.ClientPage.ClientRequest.Form[GetID("selectedId")];
                    if (_selectedId == null)
                    {
                        _selectedId = string.Empty;
                    }
                }
                return _selectedId;
            }
            set
            {
                _selectedId = value;
            }
        }

        #endregion Properties

        #region Methods

        /// <summary>
        /// Handles Messages/Events
        /// </summary>
        /// <param name="message"></param>
        public override void HandleMessage(Message message)
        {
            Assert.ArgumentNotNull(message, "message");
            base.HandleMessage(message);
            if ((message["id"] == ID) && (message.Name != null))
            {
                if (string.IsNullOrEmpty(SelectedId) && message.Name != "childlist:add")
                    SheerResponse.Alert("No element has been selected");
                else
                {
                    try
                    {
                        switch (message.Name)
                        {
                            case "childlist:moveup":
                                MoveChild(SelectedId, "up");
                                break;
                            case "childlist:movedown":
                                MoveChild(SelectedId, "down");
                                break;
                            case "childlist:edit":
                                EditChild(SelectedId);
                                break;
                            case "childlist:add":
                                AddNewChild(SelectedId);
                                break;
                            case "childlist:remove":
                                RemoveChild(SelectedId);
                                break;
                        }
                    }
                    catch(Sitecore.Exceptions.AccessDeniedException accessException)
                    {
                        SheerResponse.Alert("You did not have the necessary rights to edit this item: " + accessException.Message);
                    }
                }
            }
        }

        /// <summary>
        /// Renders the control
        /// </summary>
        /// <param name="output"></param>
        protected override void DoRender(HtmlTextWriter output)
        {
            string strEnabled = string.Empty;
            if (this.ReadOnly)
            {
                strEnabled = " disabled=\"disabled\"";
            }

            output.Write("<input type=\"hidden\" id=\"" + GetID("selectedId") + "\" value=\"\"/>");
            output.Write("<input type=\"hidden\" id=\"" + GetID("Value") + "\" value=\"\"/>");
            output.Write("<table class=\"scContentControl\" id=\"" + this.ID + "\">");
            output.Write("<tr>");
            output.Write("<td valign=\"top\" height=\"100%\" width=\"100%\">");
            output.Write(GetSelectFieldHtml(strEnabled));
            output.Write("</td>");

            output.Write("<td valign=\"top\" style=\"width:30px;\">");
            this.RenderButton(output, "Core/16x16/arrow_blue_up.png", "javascript:scForm.postEvent(this,event,'childlist:moveup(id=" + this.ID + ")');");
            output.Write("<br />");
            this.RenderButton(output, "Core/16x16/arrow_blue_down.png", "javascript:scForm.postEvent(this,event,'childlist:movedown(id=" + this.ID + ")');");
            output.Write("</td>");
            output.Write("</tr>");

            output.Write("<tr>");
            output.Write("<td valign=\"top\">");
            output.Write("<div style=\"border:1px solid #999999;font:8pt tahoma;padding:2px;margin:4px 0px 4px 0px;height:14px\" id=\"" + GetID("selected_help") + "\"></div>");
            output.Write("</td>");
            output.Write("</tr>");
            output.Write("</table>");

            base.DoRender(output);
        }

        /// <summary>
        /// Adds a new child to the currentItem
        /// </summary>
        /// <param name="siblingAboveId"></param>
        private void AddNewChild(string siblingAboveId)
        {
            Language lang = GetEditingContextLanguage();
            Item currentItem = Sitecore.Context.ContentDatabase.GetItem(this.ItemID, lang);
            AddChildController childController = new AddChildController();
            childController.AddNewChild(currentItem, lang);
        }

        /// <summary>
        /// Opens a new window with the selected element where it can be edited
        /// </summary>
        /// <param name="id"></param>
        private void EditChild(string id)
        {
            if(string.IsNullOrEmpty(id))
            {
                return;
            }
            Language lang = GetEditingContextLanguage();
            Item itemToEdit = Sitecore.Context.ContentDatabase.GetItem(id, lang);
            if (itemToEdit == null)
            {
                SheerResponse.Alert("Item with id: " + id + " could not be found");
            }

            Sitecore.Text.UrlString url = new Sitecore.Text.UrlString("/sitecore/shell/Applications/Content%20Manager/default.aspx");
            url.Append("fo", id);
            url.Append("vs", "1");
            url.Append("mo", "preview");
            url.Append("la", lang.Name);

            Sitecore.Context.ClientPage.ClientResponse.ShowModalDialog(url.ToString(), "960px", "800px", string.Empty, false);
        }

        /// <summary>
        /// Gets all children which should be listed in the ChildlistField
        /// </summary>
        /// <returns></returns>
        private List<Item> GetChildren()
        {
            List<Item> children = new List<Item>();

            Language lang = GetEditingContextLanguage();
            Item currentItem = Sitecore.Context.ContentDatabase.GetItem(this.ItemID, lang);
            List<string> templateIDs = new List<string>();
            if (currentItem.Template.StandardValues == null || currentItem.Template.StandardValues.Fields[Sitecore.FieldIDs.Branches] == null)
            {
                return children;
            }

            string insertOptions = currentItem.Template.StandardValues.Fields[Sitecore.FieldIDs.Branches].Value;
            templateIDs.AddRange(insertOptions.Split('|'));

            // Find available children and populate the lists
            foreach (Item child in currentItem.Children)
            {
                // filter away children that we are not able to create
                if(!templateIDs.Contains(child.Template.ID.ToString()))
                {
                    continue;
                }

                children.Add(child);
            }
            return children;
        }

        /// <summary>
        /// Editor language must be taken into account when adding new Items to the list for setting the correct language context based on the editing language and not the site language.
        /// </summary>
        /// <returns></returns>
        private Language GetEditingContextLanguage()
        {
            var editorDataContext = GetDataContext();
            
            // if the EditorDataContext cannot be found (e.g. in PageEdit mode) the general Context Language is used.
            return editorDataContext != null ? GetDataContext().CurrentItem.Language : Sitecore.Context.Language;
        }

        private new DataContext GetDataContext()
        {
            return Sitecore.Context.ClientPage.FindSubControl("ContentEditorDataContext") as DataContext;
        }

        /// <summary>
        /// Creates the HTML necessary for the Select field displaying all the children
        /// (also used when updating the field by setting its outerhtml)
        /// </summary>
        /// <param name="strEnabled"></param>
        /// <returns></returns>
        private string GetSelectFieldHtml(string strEnabled)
        {
            StringBuilder output = new StringBuilder();
            output.Append("<select id=\"" + GetID("selected") + "\" class=\"scContentControlMultilistBox\" multiple=\"multiple\" size=\"10\"" + strEnabled + " onchange=\"javascript:document.getElementById('" + this.ID + "_selected_help').innerHTML=this.selectedIndex>=0?this.options[this.selectedIndex].innerHTML:'';document.getElementById('" + GetID("selectedId") + "').value = this.options[this.selectedIndex].value\">");

            List<Item> children = GetChildren();
            foreach (Item child in children)
            {
                string selected = string.Empty;
                if (SelectedId == child.ID.ToString())
                {
                    selected = " selected=\"selected\"";
                }

                output.Append("<option value=\"" + child.ID.ToString() + "\"" + selected + ">" + child.Name + "</option>");
            }
            output.Append("</select>");
            return output.ToString();
        }

        /// <summary>
        /// Moves a selected child
        /// </summary>
        /// <param name="id"></param>
        /// <param name="direction">Can be either "up" or "down"</param>
        private void MoveChild(string id, string direction)
        {
            Item itemToSwap = null;
            Item itemBefore = null;
            Item itemAfter = null;
            int index = 0;
            foreach(Item child in Children)
            {
                itemAfter = child;
                if(itemToSwap != null)
                {
                    break;
                }

                if (child.ID.ToString() == id)
                {
                    itemToSwap = child;
                    index++;
                    if(direction == "up")
                    {
                        break;
                    }
                }
                itemBefore = child;
            }
            Item swapWith = (direction=="up")?itemBefore:itemAfter;
            if(swapWith == null || itemToSwap == null || swapWith.ID == itemToSwap.ID)
            {
                return;
            }

            if (itemToSwap.Fields[FieldIDs.Sortorder].Value == "0" || swapWith.Fields[FieldIDs.Sortorder].Value == "0" || string.IsNullOrWhiteSpace(itemToSwap.Fields[FieldIDs.Sortorder].Value) || string.IsNullOrWhiteSpace(swapWith.Fields[FieldIDs.Sortorder].Value))
            {
                SetNewSortordersOnChildren();
            }

            string tmpSortorder = itemToSwap.Fields[FieldIDs.Sortorder].Value;
            itemToSwap.Editing.BeginEdit();
            itemToSwap.Fields[FieldIDs.Sortorder].Value = swapWith.Fields[FieldIDs.Sortorder].Value;
            itemToSwap.Editing.EndEdit();
            swapWith.Editing.BeginEdit();
            swapWith.Fields[FieldIDs.Sortorder].Value = tmpSortorder;
            swapWith.Editing.EndEdit();

            // We update the List
            SheerResponse.SetOuterHtml(GetID("selected"), GetSelectFieldHtml(string.Empty));
        }

        /// <summary>
        /// Removes the selected child
        /// </summary>
        /// <param name="id"></param>
        private void RemoveChild(string id)
        {
            if(string.IsNullOrEmpty(id))
            {
                return;
            }

            Language lang = GetEditingContextLanguage();
            Item itemToRemove = Sitecore.Context.ContentDatabase.GetItem(id, lang);
            if(itemToRemove == null)
            {
                SheerResponse.Alert("Item with id: " + id + " could not be found");
                return;
            }

            itemToRemove.Delete();
            SheerResponse.SetOuterHtml(GetID("selected"), GetSelectFieldHtml(string.Empty));
            SheerResponse.SetAttribute(GetID("SelectedId"), "value", string.Empty);
        }


        /// <summary>
        /// Helper method to render a button
        /// </summary>
        /// <param name="output"></param>
        /// <param name="icon"></param>
        /// <param name="click"></param>
        private void RenderButton(HtmlTextWriter output, string icon, string click)
        {
            Assert.ArgumentNotNull(output, "output");
            Assert.ArgumentNotNull(icon, "icon");
            Assert.ArgumentNotNull(click, "click");
            ImageBuilder builder = new ImageBuilder();
            builder.Src = icon;
            builder.Width = 0x10;
            builder.Height = 0x10;
            builder.Margin = "2px";

            if (!this.ReadOnly)
            {
                builder.OnClick = click;
            }

            output.Write(builder.ToString());
        }

        /// <summary>
        /// Sitecore apply 0 as default sortorder value to all new items, so if the value is 0 on one of the items we need to supply a sortorder for all children in order move them
        /// </summary>
        private void SetNewSortordersOnChildren()
        {
            int sortOrder = 100;
            foreach(Item child in Children)
            {
                child.Editing.BeginEdit();
                child.Fields[FieldIDs.Sortorder].Value = sortOrder.ToString();
                child.Editing.EndEdit();
                sortOrder += 100;
            }
        }

        #endregion Methods
    }
}