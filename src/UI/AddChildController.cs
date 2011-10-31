namespace SitecoreExtension.ChildlistField.UI
{
    using System;
    using System.Collections.Specialized;

    using Sitecore.Data;
    using Sitecore.Data.Items;
    using Sitecore.Globalization;
    using Sitecore.Web.UI.Sheer;

    /// <summary>
    /// Helper class that handles the various dialogs used for adding a new child
    /// </summary>
    public class AddChildController
    {
        #region Methods

        /// <summary>
        /// Entry method used to add a new child
        /// </summary>
        /// <param name="parentItem"></param>
        /// <param name="lang"></param>
        public void AddNewChild(Item parentItem, Language lang)
        {
            Sitecore.Context.ClientPage.ClientResponse.CheckModified(false);
            NameValueCollection parameters = new NameValueCollection();
            parameters["id"] = parentItem.ID.ToString();
            parameters["database"] = parentItem.Database.Name;
            parameters["la"] = lang.Name;

            Sitecore.Context.ClientPage.Start(this, "ExecNewChildPipeline", parameters);
        }

        /// <summary>
        /// Handles the edit-item dialog afte the new child has been created
        /// </summary>
        /// <param name="args"></param>
        protected void ExecEditContentPipeline(ClientPipelineArgs args)
        {
            if (args.IsPostBack)
            {
                Database masterDb = Sitecore.Configuration.Factory.GetDatabase("master");
                Item item = masterDb.GetItem(args.Parameters["id"]);
                string parentId = item.Parent.ID.ToString();

                Sitecore.Context.ClientPage.SendMessage(this, "item:load(id=" + parentId + ")");
                Sitecore.Context.ClientPage.SendMessage(this, "item:refreshchildren(id=" + parentId + ")");
            }
            else
            {
                Sitecore.Text.UrlString url = new Sitecore.Text.UrlString("/sitecore/shell/Applications/Content%20Manager/default.aspx");
                url.Append("fo", args.Parameters["id"]);
                url.Append("vs", "1");
                url.Append("mo", "preview");
                url.Append("la", args.Parameters["la"]);

                Sitecore.Context.ClientPage.ClientResponse.ShowModalDialog(url.ToString(), "960px", "800px", string.Empty, true);
                args.WaitForPostBack(true);
            }
        }

        /// <summary>
        /// Handles the AddChildElement dialog
        /// </summary>
        /// <param name="args"></param>
        protected void ExecNewChildPipeline(ClientPipelineArgs args)
        {
            if (args.IsPostBack)
            {
                if ((!String.IsNullOrEmpty(args.Result)) && args.Result != "undefined")
                {
                    args.Parameters["id"] = args.Result;
                    Sitecore.Context.ClientPage.Start(this, "ExecEditContentPipeline", args.Parameters);
                }
            }
            else
            {
                Sitecore.Text.UrlString url = new Sitecore.Text.UrlString(Sitecore.UIUtil.GetUri("control:AddChildElement"));
                url.Append("id", args.Parameters["id"]);
                url.Append("database", args.Parameters["database"]);
                url.Append("lang", args.Parameters["la"]);

                Sitecore.Context.ClientPage.ClientResponse.ShowModalDialog(url.ToString(), "175px", "230px", string.Empty, true);
                args.WaitForPostBack(true);
            }
        }

        #endregion Methods
    }
}