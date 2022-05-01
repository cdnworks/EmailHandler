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
        public async Task SendEmail(string host, int port, string userName, string userEmailAddress, string userEmailPassword, string recipientEmailAddress, string subject, string messageBody)
        {

            //build the message from inputs with MimeKit
            MimeMessage message = await Task.Run(() => BuildMessage(userName, userEmailAddress, userEmailPassword, recipientEmailAddress, subject, messageBody));

            //send using MailKit!
            ConnectAndSend(host, port, userEmailAddress, userEmailPassword, recipientEmailAddress, message);

        }


        private MimeMessage BuildMessage(string userName, string userEmailAddress, string userEmailPassword, string recipientEmailAddress, string subject, string messageBody)
        {
            //create a new mime message object
            var message = new MimeMessage();

            //create a new message BodyBuilder object
            //the MimeKit BodyBuilder class is used for it's easy string conversion method, useful for logging
            var builder = new BodyBuilder();

            //add sender info
            message.From.Add(new MailboxAddress(userName, userEmailAddress));

            //add recipient info
            message.To.Add(MailboxAddress.Parse(recipientEmailAddress));

            //Message subject
            message.Subject = subject;

            //message body, only sends plain text (via string) with the TextBody property, though BodyBuilder allows HTML.
            builder.TextBody = messageBody;

            //set the message body
            message.Body = builder.ToMessageBody();


            return message;
        }




        private void ConnectAndSend(string host, int port, string userEmailAddress, string userEmailPassword, string recipientEmailAddress, MimeMessage message)
        {

            //pass message status to log at the end of the loop
            string sendStatus = "N/A";


            //We can give the email sender up to N attempts to connect and send, on the last attempt, the send status is collected.
            //we could log each failure as it happened, making use of some of these wasted operations but its not really needed.
            for (int i = 0; i < 3; i++)
            {

                //set up SMTP client, try to connect to the SMTP server and send the message.
                //hold error info in the case of failure
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
                            sendStatus = $"Invalid user name or password. Message Not Sent";
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
                        client.Send(message);
                        //if the message sends without problems, break the loop.
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

            LogMessage(userEmailAddress, recipientEmailAddress, message.Subject, message.Body.ToString(), sendStatus);

        }




        private void LogMessage(string userName, string toAddress, string messageSubject, string messageBody, string sendStatus)
        {
            using (StreamWriter writer = File.AppendText("log.txt"))
            {
                //set up text writer, using append text since I'd rather have one long file than a billion short ones created

                //log format
                writer.Write("Log Entry : ");
                writer.WriteLine($"{DateTime.Now.ToLongTimeString()} {DateTime.Now.ToLongDateString()}");
                writer.WriteLine($"Message Sent Status: {sendStatus}");
                writer.WriteLine($"From:    {userName}");
                writer.WriteLine($"To:      {toAddress}");
                writer.WriteLine($"Subject: {messageSubject}");
                writer.WriteLine($"{messageBody}");
                writer.WriteLine("------------------------------------------------------------------------------------------------------------------");
            }

        }





    }
}