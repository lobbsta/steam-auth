using System;
using SteamAuth;
using CommandLine;

namespace AuthTool
{
    class MainClass
    {
        public class Options
        {
            [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
            public bool Verbose { get; set; }

            [Option('u', "username", Required = true, HelpText = "Steam username")]
            public string Username { get; set; }

            [Option('p', "password", Required = true, HelpText = "Steam password")]
            public string Password { get; set; }

            [Option('n', "phone-number", Required = false, HelpText = "Phone number")]
            public string PhoneNumber { get; set; }

            [Value(0, MetaName = "action", HelpText = "Action")]
            public string Action { get; set; }
        }

        public static UserLogin Login(string username, string password)
        {
            var user = new UserLogin(username, password);
            var loginResult = user.DoLogin();
            if (loginResult != LoginResult.LoginOkay)
            {
                Console.WriteLine("Login failed with result \"{0}\"", loginResult);
                return null;
            }

            return user;
        }

        public static void Activate(UserLogin user, string phoneNumber)
        {
            Console.WriteLine("Activating 2FA for user \"{0}\" with phone \"{1}\"", user.Username, phoneNumber);

            var auth = new AuthenticatorLinker(user.Session);
            auth.PhoneNumber = phoneNumber;

            var linkResult = auth.AddAuthenticator();
            Console.WriteLine("AddAuthenticator() returned \"{0}\"", linkResult);

            if (linkResult != AuthenticatorLinker.LinkResult.AwaitingFinalization)
            {
                return;
            }

            Console.WriteLine("Waiting for SMS code...");

            var finalizeResult = auth.FinalizeAddAuthenticator(Console.ReadLine());
            Console.WriteLine("FinalizeAddAuthenticator() returned \"{0}\"", finalizeResult);

            if (finalizeResult == AuthenticatorLinker.FinalizeResult.Success)
            {
                var steamGuard = auth.LinkedAccount;
                Console.WriteLine("SharedSecret = \"{0}\"", steamGuard.SharedSecret);
                Console.WriteLine("RevocationCode = \"{0}\"", steamGuard.RevocationCode);
            }
        }

        public static void Main(string[] args)
        {
           Parser.Default.ParseArguments<Options>(args)
           .WithParsed<Options>(o =>
           {
               var user = Login(o.Username, o.Password);
               if (user == null)
               {
                  return;
               }

               Console.WriteLine("Steam login successful");

               if (o.Action == "activate")
               {
                   if (o.PhoneNumber == null || o.PhoneNumber.Length == 0)
                   {
                       Console.WriteLine("Must provide --phone-number");
                       return;
                   }

                   Activate(user, o.PhoneNumber);
               }
           });
        }
    }
}
