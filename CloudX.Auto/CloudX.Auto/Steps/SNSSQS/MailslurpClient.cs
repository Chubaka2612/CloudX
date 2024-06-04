using System;
using CloudX.Auto.Core.Utils;
using log4net;
using mailslurp.Api;
using mailslurp.Client;
using mailslurp.Model;

namespace CloudX.Auto.Tests.Steps.SNSSQS
{
    public class MailslurpClient
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(MailslurpClient));

        private readonly WaitForControllerApi _waitController;
        private readonly InboxControllerApi _inboxController;
        private readonly EmailControllerApi _emailController;

        public MailslurpClient(string apiKey)
        {
            var config = new Configuration();
            config.ApiKey.Add("x-api-key", apiKey);
            config.Timeout = 120000;

            _waitController = new WaitForControllerApi(config);
            _inboxController = new InboxControllerApi(config);
            _emailController = new EmailControllerApi(config);
        }

        public void DeleteAllInboxEmails(Guid inboxId)
        {
            Log.Info($"Delete All Emails of Inbox with id: {inboxId}"); 
            _inboxController.DeleteAllInboxEmails(inboxId);
        }


        public int GetInboxEmailCount(Guid inboxId)
        {
            Log.Info($"Get Emails Count of Inbox with id: {inboxId}");
            return (int)_inboxController.GetInboxEmailCount(inboxId).TotalElements;
        }

        public Email GetLatestEmailByInbox(Guid inboxId, string subject)
        {
            Log.Info($"Get Emails by inbox: {inboxId}");
    
            var count = (int)_inboxController.GetInboxEmailCount(inboxId).TotalElements;
            _waitController.WaitForEmailCount(inboxId, count + 1);

            // receive email with wait controller
            var email = _waitController.WaitForLatestEmail(inboxId, 60000, unreadOnly:true, sort: "ASC");

            AssertHelper.IsTrue(email.Subject.Contains(subject));
            return email;
        }
    }
}