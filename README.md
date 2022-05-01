# EmailHandler
 A simple DLL to add SMTP email sending functionality to your project, using MailKit

## How to build
 1. Clone this repo with:
 ```
 git clone --recursive https://github.com/cdnworks/EmailHandler.git
 ```
 2. Open the .sln with Visual Studio 2019 or newer and build.

## Build Troubleshooting
 if MailKit isnt included when loading the solution, run nuget CLI and use the following command:
 ```
 Install-Package MailKit
 ```

## Usage
 After referencing the EmailHandler DLL and including MailKit to your C# project, create a new Email() object and invoke the SendEmail method.
 For example:
 ```
 Email email = new Email();
 email.SendEmail(host, port, userName, userEmail, userPass, recipientEmail, subject, messageBody);
 ```
 
### SendEmail() Parameters
```
string host:           address to your SMTP server
int port:              the SMTP port for your server
string userName:       user's name, for signing emails
string userEmail:      The user (sender's) email account on your SMTP server
string userPass:       The user (sender's) SMTP auth password, send an empty string if your SMTP server doesnt use passwword authentication
string recipientEmail: Destination address for the email
string subject:        MIME format message email subject
string messageBody:    MIME format message plain text body, UTF-8 encoding
```

## References
 More info on MailKit can be found here: https://github.com/jstedfast/MailKit
 
## Contribution
 Feel free to make a PR if you would like to add or change anything.
