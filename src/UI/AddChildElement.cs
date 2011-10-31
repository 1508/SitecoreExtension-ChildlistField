namespace SitecoreExtension.ChildlistField.UI
{
    using System;
    using System.Collections.Generic;

    using Sitecore.Data;
    using Sitecore.Data.Items;
    using Sitecore.Diagnostics;
    using Sitecore.Globalization;
    using Sitecore.Web.UI.HtmlControls;
    using Sitecore.Web.UI.Pages;
    using Sitecore.Web.UI.Sheer;

    /// <summary>
    /// Dialog for adding a new child element
    /// (available templates are those set in the insertOptions field on the parent)
    /// </summary>
    public class AddChildElement : DialogForm
    {
        #region Fields

        /// <summary>
        /// Field Element
        /// </summary>
        protected Edit ElementName;

        /// <summary>
        /// Field Element
        /// </summary>
        protected Listview TemplateList;

        private Language _language;

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AddChildElement"/> class. 
        /// </summary>
        public AddChildElement()
        {
            this.TemplateList = new Listview();
            ElementName = new Edit();
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Gets or sets the database.
        /// </summary>
        /// <value>The database.</value>
        protected Database Database
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the language.
        /// </summary>
        /// <value>The language.</value>
        protected Language Language
        {
            get { return _language; }
            set { _language = value; }
        }

        /// <summary>
        /// Gets or sets the parent Item.
        /// </summary>
        /// <value>The parent Item.</value>
        protected Item Parent
        {
            get; set;
        }

        #endregion Properties

        #region Methods

        /// <summary>
        /// Populates the listview with the available templates
        /// </summary>
        protected virtual void BindListview()
        {
            List<string> templateIDs = new List<string>();
            if (Parent.Template.StandardValues != null && Parent.Template.StandardValues.Fields[Sitecore.FieldIDs.Branches] != null)
            {
                string insertOptions = Parent.Template.StandardValues.Fields[Sitecore.FieldIDs.Branches].Value;
                templateIDs.AddRange(insertOptions.Split('|'));
            }

            foreach (string templateIDString in templateIDs)
            {
                if(!ID.IsID(templateIDString))
                {
                    continue;
                }

                ID templateID;
                if (ID.TryParse(templateIDString, out templateID))
                {
                    TemplateItem template = Database.GetTemplate(templateID);
                    if (template != null)
                    {
                        ListviewItem listItem = new ListviewItem();
                        Sitecore.Context.ClientPage.AddControl(this.TemplateList, listItem);
                        listItem.ID = Control.GetUniqueID("I");

                        listItem.Header = template.Name;
                        listItem.Icon = template.Icon;
                        listItem.ColumnValues["description"] = template.InnerItem.Help.ToolTip;
                        listItem.Value = template.ID.ToString();
                    }
                }
            }
        }

        /// <summary>
        /// Initiates the various properties and populates the listview
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            string dbName = Sitecore.Web.WebUtil.GetQueryString("database");
            Database = Sitecore.Configuration.Factory.GetDatabase(dbName);
            string parentID = Sitecore.Web.WebUtil.GetQueryString("id");
            string lang = Sitecore.Web.WebUtil.GetQueryString("lang");

            Assert.IsTrue(Language.TryParse(lang, out _language), "Language could not be parsed");
            Parent = Database.GetItem(parentID, this.Language);

            if (!Sitecore.Context.ClientPage.IsEvent)
            {
                BindListview();
            }
        }

        /// <summary>
        /// Creates a new child if a proper template has been selected
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        protected override void OnOK(object sender, EventArgs args)
        {
            if (this.TemplateList.SelectedItems.Length == 0)
            {
                SheerResponse.Alert("Please select a template", new string[0]);
                return;
            }
            if (string.IsNullOrEmpty(ElementName.Value))
            {
                SheerResponse.Alert("Please enter a name for the element", new string[0]);
                return;
            }

            string nameError = ItemUtil.GetItemNameError(ElementName.Value);
            if (nameError != string.Empty)
            {
                SheerResponse.Alert(nameError, new string[0]);
                return;
            }

            Item newChild;
            try
            {
                newChild = Parent.Add(ElementName.Value, new TemplateItem(Database.GetItem(this.TemplateList.SelectedItems[0].Value, Language)));
            }
            catch (Exception ex)
            {
                SheerResponse.Alert(ex.Message, new string[0]);
                return;
            }

            SheerResponse.SetDialogValue(newChild.ID.ToString());
            base.OnOK(sender, args);
        }

        #endregion Methods
    }
}