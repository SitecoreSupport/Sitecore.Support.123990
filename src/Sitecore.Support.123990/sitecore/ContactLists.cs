using Sitecore.Analytics;
using Sitecore.Analytics.Tracking;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Diagnostics;
using Sitecore.Modules.EmailCampaign.Factories;
using System;
using System.Linq;
using System.Web;
using System.Web.SessionState;

namespace Sitecore.Support.EmailCampaign.Cd.sitecore
{
    public class ContactLists : IHttpHandler, IRequiresSessionState
    {
        // Fields
        private readonly EcmFactory _factory;

        // Methods
        public ContactLists() : this(EcmFactory.GetDefaultFactory())
        {
        }

        public ContactLists(EcmFactory factory)
        {
            Assert.ArgumentNotNull(factory, "factory");
            this._factory = factory;
        }

        public void ProcessRequest(HttpContext context)
        {
            bool flag = false;
            try
            {
                ID id;
                ID id2;
                string str = context.Request["lid"];
                string str2 = context.Request["cid"];
                string str3 = context.Request["action"];
                Assert.IsTrue(ID.TryParse(str, out id), "Invalid list id parameter");
                Assert.IsTrue(ID.TryParse(str2, out id2), "Invalid contact id parameter");
                Assert.IsTrue((str3 == "add") || (str3 == "remove"), "Invalid action parameter");
                string contactIdentifier = this._factory.Gateways.AnalyticsGateway.GetContactIdentifier(id2.Guid);
                if (!string.IsNullOrEmpty(contactIdentifier))
                {
                    try
                    {
                        Tracker.Initialize();
                        Tracker.StartTracking();
                        this._factory.Gateways.AnalyticsGateway.IdentifyContact(contactIdentifier);
                        switch (str3)
                        {
                            case "add":
                                flag = AddListTagToContact(Tracker.Current.Contact, id.ToString());
                                break;

                            case "remove":
                                flag = RemoveListTagFromContact(Tracker.Current.Contact, id.ToString());
                                break;
                        } 
                    }
                    finally
                    {
                        this._factory.Gateways.AnalyticsGateway.CancelCurrentPage();
                    }
                }
            }
            finally
            {
                context.Response.Write(flag);
                ContactManager manager = Factory.CreateObject("tracking/contactManager", true) as ContactManager;
                manager.FlushContactToXdb(Tracker.Current.Contact);
                manager.SaveAndReleaseContact(Tracker.Current.Contact);
                HttpContext.Current.Session.Abandon();
            }
        }
        internal static bool AddListTagToContact(Contact contact, string listId)
        {
            Assert.ArgumentNotNull(contact, "contact");
            Assert.ArgumentNotNull(listId, "listId");
            bool flag1 = !contact.Tags.GetAll("ContactLists").Any<string>(new Func<string, bool>(listId.Equals));
            if (flag1)
            {
                contact.Tags["ContactLists"]= listId;
            }
            return flag1;
        }
        internal static bool RemoveListTagFromContact(Contact contact, string listId)
        {
            Assert.ArgumentNotNull(contact, "contact");
            Assert.ArgumentNotNull(listId, "listId");
            bool flag1 = contact.Tags.GetAll("ContactLists").Any<string>(new Func<string, bool>(listId.Equals));
            if (flag1)
            {
                contact.Tags.Remove("ContactLists", listId);
            }
            return flag1;
        }



        // Properties
        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }
}