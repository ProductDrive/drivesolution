using MimeKit;
using MailKit.Net.Smtp;
using NotificationDomain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotificationWorker
{
    public class EmailEngine
    {

        public static bool SendEmail(MessageDTO msgModel)
        {
            var msg = BuildEmailMessage(msgModel);
            return SendReadyEmail(msg, msgModel.SenderSettings, msgModel.FallBackSenderSettings);
        }

        public static List<bool> SendMultipleEmail(List<MessageDTO> msgModel, SenderSettingsDTO sender, SenderSettingsDTO fallBackSender)
        {
            List<MimeMessage> messages = new List<MimeMessage>();
            foreach (var item in msgModel)
            {
                messages.Add(BuildEmailMessage(item));
            }


            Task<bool>[] tasks = messages.Select(message => Task<bool>.Factory.StartNew(() => SendReadyEmail(message, sender, fallBackSender))).ToArray();
            Task.WaitAll(tasks);
            //then add the result of all the tasks to sentResult in a threadsafe fashion
            List<bool> sentResult = tasks.Select(task => task.Result).ToList();

            return sentResult;
        }

        public static List<SenderSettingsDTO> AuthenticateSender(string publickey)
        {
            throw new NotImplementedException();
            //// Handle specified domain
            //if (!string.IsNullOrWhiteSpace(domain))
            //{
            //    CommonHosts specified = new CommonHosts
            //    {
            //        Domain = domain,
            //        Ports = new int[] { port },
            //        ServerType = "specified",
            //        ServiceName = "Custome"
            //    };
            //    bool isAuth = Authenticator(emailaddress, password, specified);
            //    if (isAuth)
            //        return new List<SenderSettings>() { new SenderSettings { Domain = domain, Email = emailaddress, Password = password, Port = port } };


            //    return null;
            //}

            ////Handle anonymous domain
            //CommonHosts anonymuosdomain = new CommonHosts
            //{
            //    Domain = emailaddress.Split("@")[1],
            //    Ports = Secretes.GetPorts(),
            //    ServerType = "anon",
            //    ServiceName = "Custom"
            //};

            ////Add anonymous domains to default domains
            //commonHosts.Add(anonymuosdomain);

            //if (string.IsNullOrWhiteSpace(emailaddress) && !emailaddress.Contains("@"))
            //    return null;

            //if (string.IsNullOrWhiteSpace(password))
            //    return null;

            ////Iterate all hosts to get the hosts to use for auth. Default host is added already
            //List<CommonHosts> hostForAuth = new List<CommonHosts>();
            //foreach (var host in commonHosts)
            //{
            //    List<CommonHosts> hostPerPort = AuthDetails(host);
            //    hostForAuth.AddRange(hostPerPort);
            //}

            //// tests all ports
            //Task<bool>[] tasks = hostForAuth.Select(a => Task<bool>.Factory.StartNew(() => Authenticator(emailaddress, password, a))).ToArray();
            //Task.WaitAll(tasks);
            ////then add the result of all the tasks to r in a treadsafe fashion
            //List<bool> authResult = tasks.Select(task => task.Result).ToList();

            //// pick those that connect and return the connection settings
            //List<SenderSettings> connectionList = new List<SenderSettings>();

            //for (int i = 0; i < authResult.Count; i++)
            //{
            //    if (authResult[i])
            //    {
            //        connectionList.Add(
            //            new SenderSettings
            //            {
            //                Domain = hostForAuth[i].Domain,
            //                Email = emailaddress,
            //                Password = password,
            //                Port = hostForAuth[i].Ports[0]
            //            });
            //    }
            //}
            //return connectionList;
        }

        private static MimeMessage BuildEmailMessage(MessageDTO msgModel)
        {
            MimeMessage message = new MimeMessage();
            BodyBuilder builder = new BodyBuilder();
            if (msgModel.Attachments != null && msgModel.Attachments.Count > 0)
            {
                foreach (var attachment in msgModel.Attachments)
                {
                    builder.Attachments.Add(
                        attachment.FileName,
                        attachment.FileBytes,
                        ContentType.Parse(attachment.ContentType)
                    );
                }
            }

            builder.HtmlBody = msgModel.Message;

            message.Body = builder.ToMessageBody();
            message.From.Add(new MailboxAddress(msgModel.EmailDisplayName, msgModel.SenderSettings.Email));
            message.ReplyTo.Add(new MailboxAddress(msgModel.EmailDisplayName, msgModel.ReplyTo ?? msgModel.SenderSettings.Email));
            msgModel.Contacts?.ToList().ForEach(contact => message.To.Add(MailboxAddress.Parse(contact.Email)));
            msgModel.Bcc?.ToList().ForEach(x => message.Bcc.Add(MailboxAddress.Parse(x)));
            msgModel.Cc?.ToList().ForEach(x => message.Cc.Add(MailboxAddress.Parse(x)));
            message.Subject = string.IsNullOrWhiteSpace(msgModel.Subject) ? "(no Subject)" : msgModel.Subject;
            
            return message;
        }

        //private static List<CommonHosts> AuthDetails(CommonHosts oneDetails)
        //{
        //    List<CommonHosts> domainlist = new List<CommonHosts>();
        //    foreach (var item in oneDetails.Ports)
        //    {
        //        domainlist.Add(new CommonHosts
        //        {
        //            Domain = oneDetails.Domain,
        //            Ports = new int[] { item },
        //            ServerType = oneDetails.ServerType,
        //            ServiceName = oneDetails.ServiceName
        //        });
        //    }
        //    return domainlist;
        //}


        //private static bool Authenticator(string emailaddress, string password, CommonHosts details)
        //{
        //    SmtpClient smtpClient = new SmtpClient();
        //    try
        //    {

        //        smtpClient.Connect(details.Domain, details.Ports.First(), false);
        //        smtpClient.Authenticate(emailaddress, password);
        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        return false;
        //    }
        //}

        private static bool SendReadyEmail(MimeMessage message, SenderSettingsDTO sender, SenderSettingsDTO fallBackSender)
        {
            
            SmtpClient smtpClient = new SmtpClient();
            sender.Port = 465;
            var isSent = false;
            try
            {

                smtpClient.Connect(sender.Domain, sender.Port, MailKit.Security.SecureSocketOptions.Auto);
                smtpClient.Authenticate(sender.Email, sender.Password);
                smtpClient.Send(message);
                isSent = true;
            }
            catch (Exception ex)
            {
                string errInfor = ex.Message;
                isSent = false;
            }
            finally
            {

                //smtpClient.Disconnect(true);
                //smtpClient.Dispose();
                if (smtpClient.IsConnected && isSent)
                {
                    smtpClient.Disconnect(true);
                    smtpClient.Dispose();
                }
                else if (smtpClient.IsConnected && !isSent)
                {
                    var failedSubject = message.Subject;
                    smtpClient.Disconnect(true);
                    smtpClient.Connect(fallBackSender.Domain, fallBackSender.Port, MailKit.Security.SecureSocketOptions.Auto);
                    smtpClient.Authenticate(fallBackSender.Email, fallBackSender.Password);
                    message.From.Clear();
                    message.From.Add(new MailboxAddress("Haziel from Product Drive", "responds@elberith.org"));
                    message.To.Clear();
                    message.To.Add(MailboxAddress.Parse(fallBackSender.Email));
                    message.To.Add(MailboxAddress.Parse("afeexclusive@gmail.com"));
                    message.To.Add(MailboxAddress.Parse("productdrive@proton.me"));
                    message.Subject = $"Email sending failed::sender {sender.Email}::{failedSubject}";
                    smtpClient.Send(message);
                    smtpClient.Disconnect(true);
                    smtpClient.Dispose();

                }
                else
                {
                    smtpClient.Connect(fallBackSender.Domain, fallBackSender.Port, MailKit.Security.SecureSocketOptions.Auto);
                    smtpClient.Authenticate(fallBackSender.Email, fallBackSender.Password);
                    message.From.Clear();
                    //message.From.Add(new MailboxAddress("responds@elberith.org", "responds@elberith.org"));
                    message.From.Add(new MailboxAddress("Haziel from Product Drive", "responds@elberith.org"));
                    message.To.Clear();
                    message.To.Add(message.Bcc?.First());
                    message.To.Add(MailboxAddress.Parse("afeexclusive@gmail.com"));
                    message.To.Add(MailboxAddress.Parse("productdrive@proton.me"));
                    message.Subject = $"Email sending failed::sender{sender.Email}";
                    smtpClient.Send(message);
                    smtpClient.Disconnect(true);
                    smtpClient.Dispose();
                }

            }
            return isSent;
        }
    }
}
