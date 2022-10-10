using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace EmailHandler
{
    public class Email
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string UserName { get; set; }
        public string UserEmail { get; set; }
        private string UserPassword { get; set; }   //my intuition says there is a smarter and safer way to handle passwords; look into it later
        public string RecipientEmail { get; set; }
        public string MessageSubject { get; set; }
        public string MessageBody { get; set; }
        private string SendStatus { get; set; }     //this property is null until SendMessage is called.

        public Email(string host, int port, string userName, string userEmail, string userPassword, string recipientEmail, string messageSubject, string messageBody)
        {
            Host = host;
            Port = port;
            UserName = userName;
            UserEmail = userEmail;
            UserPassword = userPassword;
            RecipientEmail = recipientEmail;
            MessageSubject = messageSubject;
            MessageBody = messageBody;
        }


        //This public method is what needs to be called to actually send the email.
        //Once an Email object is construted properly, and this method is called,
        //The Email is composed into a mimemessage, sent and a log is generated.
        public void SendEmail()
        {
            MimeMessage message = BuildMessage();

            SendMessage(message);

            LogMessage();
        }


        private MimeMessage BuildMessage()
        {
            var message = new MimeMessage();

            //the MimeKit BodyBuilder class is used for it's easy string conversion method, useful for logging
            var builder = new BodyBuilder();
            message.From.Add(new MailboxAddress(UserName, UserEmail));
            message.To.Add(MailboxAddress.Parse(RecipientEmail));
            message.Subject = MessageSubject;
            builder.TextBody = MessageBody;
            message.Body = builder.ToMessageBody();
            return message;
        }


        private void SendMessage(MimeMessage emailMessage)
        {
            //return value for message sendStatus
            string sendStatus = "N/A";

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
                        client.Connect(Host, Port, SecureSocketOptions.SslOnConnect);
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
                            client.Authenticate(UserEmail, UserPassword);
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
            SendStatus = sendStatus;
        }


        private void LogMessage()
        {
            using (StreamWriter writer = File.CreateText("testing.txt")) //creates a sortable .txt log file named after the time it was sent
            {
                writer.Write("Log Entry : ");
                writer.WriteLine($"{DateTime.Now.ToLongTimeString()} {DateTime.Now.ToLongDateString()}");
                writer.WriteLine($"Message Sent Status: {SendStatus}");
                writer.WriteLine($"From:    {UserName}");
                writer.WriteLine($"To:      {RecipientEmail}");
                writer.WriteLine($"Subject: {MessageSubject}");
                writer.WriteLine($"{MessageBody}");
                writer.WriteLine("------------------------------------------------------------------------------------------------------------------");
            }
        }
    }
}