using System;
using MimeKit;
using MailKit.Net.Smtp;

namespace Common.Functional.Mail
{
    public class MailF
    {
        private MailboxAddress hostMail;
        private string ip = "127.0.0.1";
        private string mailAddress = "";
        private string mailPassword = "";
        private readonly string GmailServer = "smtp.gmail.com";
        private readonly int GmailPort = 587;

        public MailF()
        {
            this.ip = Config.GetConfigValue("ip", TypeCode.String);
            this.mailAddress = Config.GetConfigValue("mail_address", TypeCode.String);
            this.mailPassword = Config.GetConfigValue("mail_password", TypeCode.Single);
            if (ip != null && mailAddress != null)
            {
                hostMail = new MailboxAddress(ip, mailAddress);
            }
        }
        public void SendEmail(string emailAddress, string subject, string message)
        {
            MimeMessage emailMessage = new MimeMessage();
            emailMessage.From.Add(hostMail);
            emailMessage.To.Add(new MailboxAddress(emailAddress));
            emailMessage.Subject = subject;
            emailMessage.Body = new TextPart(MimeKit.Text.TextFormat.Html)
            {
                Text = message
            };
            try
            {
                using (SmtpClient client = new SmtpClient())
                {
                    client.Connect(GmailServer, GmailPort, false);
                    client.Authenticate(hostMail.Address, mailPassword);
                    client.Send(emailMessage);
                    client.DisconnectAsync(true);
                    Logger.WriteLog("Send message to " + emailAddress, LogLevel.Usual);
                }
            }
            catch (Exception e)
            {
                Logger.WriteLog("Error SendEmailAsync, Message:" + e.Message, LogLevel.Error);
            }
        }
    }
}

