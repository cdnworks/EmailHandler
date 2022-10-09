using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace EmailHandler
{
    public class Email
    {
        //This class pulls a number of input parameters to construct a MIME formatted email message, which is then sent using SMTP.
        //the message is logged with a timestamp after a successful send, or after attempting and failing to send up to 3 times.
        //The operations in SendEmail must happen asynchronously, as per the project spec.
        public async Task SendEmail(string host, int port, string userName, string userEmailAddress, string userEmailPassword, string recipientEmailAddress, string messageSubject, string messageBody)
        {
            //build the message from inputs with MimeKit
            MimeMessage message = await Task.Run(() => BuildMessage(userName, userEmailAddress, recipientEmailAddress, messageSubject, messageBody));

            string sendStatus = SendMessage(host, port, userEmailPassword, message);

            LogMessage(userName, recipientEmailAddress, messageSubject, messageBody, sendStatus);

        }


        private MimeMessage BuildMessage(string userName, string userEmailAddress, string recipientEmailAddress, string subject, string messageBody)
        {
            var message = new MimeMessage();

            //the MimeKit BodyBuilder class is used for it's easy string conversion method, useful for logging
            var builder = new BodyBuilder();
            message.From.Add(new MailboxAddress(userName, userEmailAddress));
            message.To.Add(MailboxAddress.Parse(recipientEmailAddress));
            message.Subject = subject;
            builder.TextBody = messageBody;
            message.Body = builder.ToMessageBody();
            return message;
        }


        private string SendMessage(string host, int port, string userEmailPassword, MimeMessage emailMessage)
        {
            //return value for message sendStatus
            string sendStatus = "N/A";

            //get info from message
            string userEmailAddress = emailMessage.From.ToString();
            string recipientEmailAddress = emailMessage.To.ToString();

            //The problem spec included needing to make up to 3 attempts at sending the message before aborting the send
            for (int i = 0; i < 3; i++)
            {
                //set up SMTP client, try to connect to the SMTP server and send the message.
                //the client is destroyed after each attempt regardless if it was successful or not
                //this terminates the connection (if successful) and prevents bad behaviors on further attempts.
                using (var client = new SmtpClient())
                {
                    //try to connect to the SMTP server
                    try
                    {
                        client.Connect(host, port, SecureSocketOptions.SslOnConnect);
                    }
                    catch (SmtpCommandException ex)
                    {
                        sendStatus = $"Error trying to connect: {ex.Message} StatusCode: {ex.StatusCode}";
                    }
                    catch (SmtpProtocolException ex)
                    {
                        sendStatus = $"Protocol error while trying to connect: {ex.Message}";
                    }
                    // check if the SMTP server uses an auth protocol
                    // Note: Not all SMTP servers support authentication, but GMail does for example.
                    if (client.Capabilities.HasFlag(SmtpCapabilities.Authentication))
                    {
                        try
                        {
                            client.Authenticate(userEmailAddress, userEmailPassword);
                        }
                        catch (AuthenticationException ex)
                        {
                            sendStatus = $"Invalid user name or password. Message Not Sent. {ex.Message}";
                        }
                        catch (SmtpCommandException ex)
                        {
                            sendStatus = $"Error trying to authenticate: {ex.Message} StatusCode: {ex.StatusCode}";
                        }
                        catch (SmtpProtocolException ex)
                        {
                            sendStatus = $"Protocol error while trying to authenticate: {ex.Message}";
                        }
                    }
                    //try to send the message
                    try
                    {
                        client.Send(emailMessage);
                        sendStatus = "Successfully Sent";
                        break;
                    }
                    catch (SmtpCommandException ex)
                    {
                        sendStatus = $"Error sending message: {ex.Message} StatusCode: {ex.StatusCode} ErrorCode: {ex.ErrorCode}";
                    }
                    catch (SmtpProtocolException ex)
                    {
                        sendStatus = $"Protocol error while sending message: {ex.Message}";
                    }

                    client.Disconnect(true);

                }
            }
            return sendStatus;
        }


        private void LogMessage(string userName, string recipientEmailAddress, string messageSubject, string messageBody, string sendStatus)
        {
            using (StreamWriter writer = File.AppendText("log.txt"))
            {
                writer.Write("Log Entry : ");
                writer.WriteLine($"{DateTime.Now.ToLongTimeString()} {DateTime.Now.ToLongDateString()}");
                writer.WriteLine($"Message Sent Status: {sendStatus}");
                writer.WriteLine($"From:    {userName}");
                writer.WriteLine($"To:      {recipientEmailAddress}");
                writer.WriteLine($"Subject: {messageSubject}");
                writer.WriteLine($"{messageBody}");
                writer.WriteLine("------------------------------------------------------------------------------------------------------------------");
            }
        }
    }
}