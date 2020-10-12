using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;
using Google.Apis.Util.Store;

namespace Gmailer
{
    class GmailMessage
    {
        public string Id { get; set; }
        public List<string> Content { get; set; }
    }

    class Program
    {
        static string[] Scopes = {GmailService.Scope.GmailReadonly};
        static string ApplicationName = "Gmailer";
        static string User = "tkachuk.pavel13@gmail.com";
        private static int MailMaxCount = 1;

        static void Main(string[] args)
        {
            var creds = ReadCredential(User);
            var service = CreateGmailService(creds);
            var messages = GetMails(service);

            if (messages == null || messages.Count == 0)
            {
                Console.WriteLine("No messages");
            }
            else
            {
                foreach (var m in messages)
                {
                    Console.WriteLine(m.Id);
                    foreach (var text in m.Content)
                    {
                        Console.WriteLine(text);
                        Console.WriteLine("===================");
                    }
                    
                }
            }

            Console.ReadKey();
        }

        static UserCredential ReadCredential(string user, string path = "credentials.json")
        {
            using (var stream =
                new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                // The file token.json stores the user's access and refresh tokens, and is created
                // automatically when the authorization flow completes for the first time.
                string credPath = "token.json";
                var credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    user,
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                Console.WriteLine("Credential file saved to: " + credPath);

                return credential;
            }
        }

        static GmailService CreateGmailService(UserCredential creds)
        {
            var service = new GmailService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = creds,
                ApplicationName = ApplicationName,
            });

            return service;
        }

        static IList<GmailMessage> GetMails(GmailService service)
        {
            var mails = new List<GmailMessage>();

            var ids = GetMailIds(service);

            if (ids == null || ids.Count == 0)
            {
                return mails;
            }

            foreach (var mId in ids)
            {
                var content = GetMailContent(service, mId);
                mails.Add(new GmailMessage()
                {
                    Id = mId,
                    Content = content
                });
            }

            return mails;
        }

        static List<string> GetMailIds(GmailService service)
        {
            var request = service.Users.Messages.List("me");
            request.LabelIds = "INBOX";
            request.MaxResults = MailMaxCount;
            // request.Q = "is:unread";

            var messages = request.Execute().Messages;

            if (messages == null || messages.Count == 0)
            {
                return new List<string>();
            }

            return messages.Select(m => m.Id).ToList();
        }

        static List<string> GetMailContent(GmailService service, string messageId)
        {
            List<string> content = new List<string>();

            var request = service.Users.Messages.Get("me", messageId);
            request.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Full;

            var message = request.Execute();

            // parse body
            var body = message.Payload.Body.Data;
            if (body != null)
            {
                content.Add(DecodeGmailData(body));
            }
            
            var parts = message.Payload.Parts;
            if (parts != null)
            {
                foreach (var part in parts)
                {
                    if (part.Body.Data != null)
                    {
                        content.Add(DecodeGmailData(part.Body.Data));
                    }
                }
            }

            return content;
        }

        static string DecodeGmailData(string source)
        {
            var converted = source.Replace('-', '+').Replace('_', '/');
            var data = Convert.FromBase64String(converted);
            return Encoding.UTF8.GetString(data);
        }
    }
}