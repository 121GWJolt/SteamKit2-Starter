using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamKit2;
using System.Diagnostics;
using System.IO;
using System.Threading;


//Original template coded by 121GWJolt.
//You may edit this code however you wish, but you must retain this note in the sourcecode of the project.
//
//Also, you may not sell this code as-is.  The code may be included in other applications that are for sale, but these credits must be preserved.
//Derivative works of this code may not be sold without significant modification, and even then must retain these credits.
//The licensing of the included SteamKit2 and protobuf-net NuGet packages take priority over this notice.  I do not own either package
//and cannot grant permission for their use in applications.
//THIS CODE IS PRESENTED WITHOUT WARRANTY AND 121GWJOLT CANNOT BE HELD RESPONSIBLE FOR ANY DAMAGES CAUSED BY USE OF THIS CODE UNDER ANY CIRCUMSTANCES.
//121GWJOLT IS NOT LEGALLY OBLIGATED TO FIX ANY ISSUES OR REPLACE ANY LOSSES RESULTING FROM USE OF THIS CODE.

namespace BasicSteamBot
{
    /// <summary>
    /// Contains the classes used to display SteamKit2's DebugLog messages.
    /// </summary>
    namespace DebugLogListeners
    {
        #if DEBUG
            /// <summary>
            /// Sends messages to VS2015's Debug window.
            /// </summary>
            class SendToDebug : IDebugListener
            {
                public void WriteLine(string category, string msg)
                {
                    Debug.WriteLine("SteamKit2> {0} : {1}", category, msg);
                }
            }
        #endif
            /// <summary>
            /// Sends messages to the console itself.
            /// </summary>
            class SendToCon : IDebugListener
            {
                public void WriteLine(string category, string msg)
                {
                    Console.WriteLine("SteamKit2> " + category + ": " + msg, Console.ForegroundColor = ConsoleColor.Green);
                    Console.ResetColor();
                }
            }
    }



    class SteamConnect
    {
        //MAKE SURE TO SET THE BOT OWNER ID TO YOUR OWN 64IDbelow!!  Don't know it? Look it up on http://www.steamid.co
        //
        public  SteamID BotOwnerID/* = xxxxxxxxxxxxxxxxxx*/;
        //
        //**************************************************************************************************************
         string strUser, strPassword;
        public  bool steamIsRunning;
         string authCode, twofactor;
         Random rnd = new Random();
        public  SteamClient steamClient;
        public  CallbackManager callManager;
        public  SteamUser steamUser;
        public  SteamFriends steamFriends;
        public  bool authed = false;
        public  JobID StandardCallBackJob = new JobID();
         bool decrwSentP = false;
         bool dlogregd = false;
        public  string RememberKey;
        public  bool RememberMe = false;
         bool ServerMode = false;


        /// <summary>
        /// Gets the SteamID from a string.
        /// </summary>
        /// <param name="strID"></param>
        /// <returns></returns>
        public  SteamID StmFUstr(string strID)
        {
            SteamID steamID = new SteamID();
            steamID.SetFromUInt64(Convert.ToUInt64(strID));
            return steamID;
        }

        private  IDebugListener ConDeLog = new DebugLogListeners.SendToCon();


        /// <summary>
        /// Toggles sending DebugLog to the Console.
        /// </summary>
        public  void ToggleDLogToCon()
        {
            if (dlogregd == false)
            {
                DebugLog.AddListener(ConDeLog);
                dlogregd = true;
#if DEBUG
                Console.WriteLine("DebugLog now outputs here!", Console.ForegroundColor = ConsoleColor.Green);
#endif
            }
            if (DebugLog.Enabled == false)
            {
                DebugLog.Enabled = true;
                Console.WriteLine("DebugLog enabled!", Console.ForegroundColor = ConsoleColor.Green);
            }
            else if (dlogregd)
            {
                DebugLog.RemoveListener(ConDeLog);
                dlogregd = false;
#if DEBUG
                Console.WriteLine("Console no longer logging DebugLog messages!  Check the debug window output instead.", Console.ForegroundColor = ConsoleColor.Green);
#elif !DEBUG
            DebugLog.Enabled = false;
            Console.WriteLine("DebugLog disabled!", Console.ForegroundColor = ConsoleColor.Green);
#endif
        }
    }

        /// <summary>
        /// Generates/retrieves the key needed to decrypt any encrypted file Chatti uses.  This includes the password and key files.  If any of the required files are missing, Chatti will crash and credidential files must be deleted so new ones can be generated.
        /// </summary>
        /// <returns></returns>
         byte[] getstandardkey()
        {
            System.Security.Cryptography.AesCryptoServiceProvider helper = new System.Security.Cryptography.AesCryptoServiceProvider();
            helper.GenerateKey();
            if (File.Exists("dispenser.bin") == false)
                File.WriteAllBytes("dispenser.bin", helper.Key);
            byte[] keyend = File.ReadAllBytes("dispenser.bin");
            byte[] sentryFile;
            if (File.Exists("sentry.bin"))
                sentryFile = File.ReadAllBytes("sentry.bin");
            else
            {
                byte[] dummysent = new byte[18];
                rnd.NextBytes(dummysent);
                File.WriteAllBytes("sentry.bin", dummysent);
                sentryFile = dummysent;
            }
            byte[] sentryHash = CryptoHelper.SHAHash(sentryFile);
            byte[] fullkey = new byte[helper.Key.Length];
            int y = 0;
            while (y < sentryHash.Length)
            {
                fullkey[y] = sentryHash[y];
                y = y + 1;
            }
            while (y < fullkey.Length)
            {
                fullkey[y] = keyend[y - 18];
                y = y + 1;
            }
            return fullkey;

        }


        /// <summary>
        /// Self-explanatory.  Automatically selects the appropriate method to use.
        /// </summary>
        /// <returns></returns>
         string GetUserName()
        {
            if (File.Exists("UserName.txt"))
            {
                return File.ReadAllText("UserName.txt");
            }
            else
            {
                Program.stallcommand = true;
                Console.WriteLine("No UserName.txt detected.  Please create one in the root application directory or enter your username in the Input Box.");
                Console.Write("Username> ");
                string username = Console.ReadLine();
                Thread.Sleep(10);
                Console.WriteLine("Type \"yes\" to save this username in a txt file.");
                Console.Write("Store?> ");
                string output = Console.ReadLine();
                if (output == "yes")
                {
                    File.WriteAllText("UserName.txt", username);
                }
                Program.stallcommand = false;
                return username;
            }
        }

        /// <summary>
        /// Grabs the password for this account.
        /// </summary>
        /// <param name="modo">Specifies what method of password access/storage to use for this attempt.</param>
        /// <param name="instantencrypt">If accessing an encrypted password, if true, will automatically encrypt a password entered via a plaintext file.  Note that if the desired method does not work, more will be tried until one does.</param>
        /// <returns></returns>
         string GetUserPass(UserPassMode modo, bool instantEncrypt = false)
        {
            if (File.Exists("UserKey.bin") && File.Exists("sentry.bin")&&modo == UserPassMode.SteamKey)
            {
                Debug.WriteLine("Using login key method.");
                RememberMe = true;
                return "password";
            }
            else if (File.Exists("UserPass.bin") && File.Exists("sentry.bin") && !(modo == UserPassMode.InputPassword || modo == UserPassMode.Plaintext))
            {
                Debug.WriteLine("Using encrypted password method.");
                decrwSentP = true;
                RememberMe = true;
                byte[] phashed = File.ReadAllBytes("UserPass.bin");
                string newKey = Encoding.Unicode.GetString(CryptoHelper.SymmetricDecrypt(phashed, getstandardkey()));
                return newKey;

            }
            else if (File.Exists("UserPass.txt")&&!(modo == UserPassMode.InputPassword))
            {
                return File.ReadAllText("UserPass.txt");
                if (instantEncrypt)
                {
                    byte[] newpass = Encoding.Unicode.GetBytes(strPassword);
                    byte[] newphashed = CryptoHelper.SymmetricEncrypt(newpass, getstandardkey());
                    File.WriteAllBytes("UserPass.bin", newphashed);
                    Console.WriteLine("UserPass.bin re-encrypted!");
                }
            }
            else
            {
                Program.stallcommand = true;
                Console.WriteLine("Please enter your password in the box below.  ");
                Console.Write("Password> ");
                ConsoleKeyInfo pkey = new ConsoleKeyInfo();
                string password = "";
                while (pkey.Key != ConsoleKey.Enter)
                {
                    pkey = Console.ReadKey(true);
                    if (pkey.Key == ConsoleKey.Enter)
                    {
                        Debug.WriteLine("Password completed.");
                    }
                    else if (pkey.Key == ConsoleKey.Backspace && password.Length == 0) ;
                    else if (pkey.Key == ConsoleKey.Backspace && password.Length == 1)
                    {
                        Console.Write(string.Join("",Enumerable.Repeat("\b", password.Length).ToArray()) + string.Join("",Enumerable.Repeat(" ", password.Length).ToArray()) + string.Join("",Enumerable.Repeat("\b", password.Length)).ToArray());
                        password = "";
                    }
                    else if (pkey.Key == ConsoleKey.Backspace && password != "")
                    {
                        Console.Write(string.Join("",Enumerable.Repeat("\b", password.Length).ToArray()) + string.Join("",Enumerable.Repeat(" ", password.Length).ToArray()) + string.Join("",Enumerable.Repeat("\b", password.Length).ToArray()));
                        password = password.Substring(0, password.Length - 1);
                        Console.Write(string.Join("",Enumerable.Repeat("*", password.Length)).ToArray());
                    }

                    else
                    {
                        Console.Write(string.Join("",Enumerable.Repeat("\b", password.Length).ToArray()) + string.Join("",Enumerable.Repeat(" ", password.Length).ToArray()) + string.Join("",Enumerable.Repeat("\b", password.Length).ToArray()));
                        password += pkey.KeyChar.ToString();
                        Console.Write(string.Join("",Enumerable.Repeat("*", password.Length).ToArray()));
                    }
                }
                Console.WriteLine("Type \"yes\" to store a login key from Steam.  This will allow you to login without credidentials as long as a key file is present.\nYou can also edit the code to store the password to an encrypted file if you so wish.");
                Console.Write("Store?> ");
                string output = Console.ReadLine();
                if (output == "yes")
                {
                    RememberMe = true;
                    if (modo == UserPassMode.EncryptedPassword) //Automatically stores the entered password as an encrypted file is present.
                    {
                        byte[] newpass = Encoding.Unicode.GetBytes(strPassword);
                        byte[] newphashed = CryptoHelper.SymmetricEncrypt(newpass, getstandardkey());
                        File.WriteAllBytes("UserPass.bin", newphashed);
                        Console.WriteLine("UserPass.bin encrypted!");
                    }
                }
                Program.stallcommand = false;
                return password;
            }
        }

        /// <summary>
        /// Enum to denote the mode <see cref="GetUserPass(UserPassMode, bool)"/>runs in.
        /// </summary>
        public enum UserPassMode
        {
            SteamKey = 0,//From UserKey.bin
            EncryptedPassword = 1,//From UserPass.bin
            Plaintext = 2,//From User.txt
            InputPassword = 3
        }

        //
        //
        //Below actually runs the program!
        // 
        //

        /// <summary>
        /// Actually starts the program.
        /// </summary>
        /// <param name="args"></param>
        public  void BeginClient(string[] args)
        {
            if (BotOwnerID == null)
            {
                System.Media.SystemSounds.Exclamation.Play();
                Debug.Assert(BotOwnerID != null, "SteamID BotOwnerID is not set to a value.  Please assign a value to it in SteamMain.cs on ~line 60.");
                Console.WriteLine("SteamID BotOwnerID is not set to a value.  Please assign a value to it in SteamMain.cs.\nPress any key to exit.");
                Console.WriteLine(Console.ReadKey());
                Environment.Exit(0);
            }
            
                
            

#if DEBUG
            DebugLog.AddListener(new DebugLogListeners.SendToDebug());
            DebugLog.Enabled = true;
#endif
            try
            {
                if (args[0] == "servermode")
                {
                    Console.WriteLine("Running as game server!");
                    ServerMode = true;
                }
            }
            catch { }
            Console.WriteLine("Ctrl+C Quits the Program", Console.BackgroundColor = ConsoleColor.White);
            Console.WriteLine("Be aware of what the input line says at the beginning.  If you see \"Command>\" at the beginning of the line, even if it gets ammended with something else, be warned that you will have to press enter before anything but commands will be accepted.  Oh, and if nothing is displayed on the typing line, then your first keypress will be ignored unless you see something appear on the line.  Be aware of this.");
            Console.ResetColor();
            strUser = GetUserName();
            strPassword = GetUserPass(UserPassMode.SteamKey, false);


            RunSteam();

        }



        /// <summary>
        /// Begins the connection process.  If you need to pass console arguments here (eg. to say whether to connect as a server or as a user or both), you can use Environment.GetCommandLineArgs() to get them rather than trying to shift them through the whole code.
        /// </summary>
        public  void RunSteam()
        {
            steamClient = new SteamClient();

            callManager = new CallbackManager(steamClient);

            steamUser = steamClient.GetHandler<SteamUser>();

            steamFriends = steamClient.GetHandler<SteamFriends>();

            //Stack of callbacks.
            callManager.Subscribe<SteamClient.ConnectedCallback>(StandardCallBackJob, OnConnected);
            callManager.Subscribe<SteamUser.LoggedOnCallback>(StandardCallBackJob, OnLoggedOn);
            callManager.Subscribe<SteamClient.DisconnectedCallback>(StandardCallBackJob, OnDisconnected);
            callManager.Subscribe<SteamFriends.FriendMsgCallback>(StandardCallBackJob, OnChatMessage);
            callManager.Subscribe<SteamUser.AccountInfoCallback>(StandardCallBackJob, OnAccountInfo);
            callManager.Subscribe<SteamUser.LoginKeyCallback>(StandardCallBackJob, OnKeyGet);
            callManager.Subscribe<SteamUser.UpdateMachineAuthCallback>(StandardCallBackJob, UpdateMachineCallback);

            steamIsRunning = true;

            Console.WriteLine("Opening Connection....", Console.ForegroundColor = ConsoleColor.Green, Console.BackgroundColor == ConsoleColor.Black);

            steamClient.Connect();

            const int callbacklimit = 100; //100 is just a suggestion, but you don't necessarily need a limit.

            while (steamIsRunning)
            {
                //This setup allows large blocks of callbacks to be run, thereafter blocking the thread until more are received.
                //It seems redundant, but when I tried to do RunCallback() alone in this loop, it just took the CPU for itself to the point my system
                //(which can handle quite a few nice games) took a performance hit.  That's too rediculous.  This fixes that.
                int counterup = 0;
                while (steamClient.GetCallback() != null && counterup < callbacklimit)
                {
                    callManager.RunCallbacks();
                    counterup++;
                }
                if (counterup > callbacklimit)
                {
                    //In case you want it to do something.  Maybe make the thread wait for a bit, or not.
                }
                steamClient.WaitForCallback();
            }
        }



     void OnConnected(SteamKit2.SteamClient.ConnectedCallback callback)
        {
            if (callback.Result == EResult.NoConnection)
            {
                Console.WriteLine("Not connected to the internet.  Gonna take a little longer than usual.", Console.ForegroundColor = ConsoleColor.Green);
                Thread.Sleep(25000);
            }
            if (callback.Result != EResult.OK)
            {
                Console.WriteLine("OH NO! I CAN'T BELIEVE IT!  Steam won't talk to me." + callback.Result, Console.ForegroundColor = ConsoleColor.Green);
                return;
            }

            Console.WriteLine("Connected to Steam.");
            Console.WriteLine("Logging in, reversing the polarity of the neutron flow!", Console.ForegroundColor = ConsoleColor.Green);

            byte[] sentryHash = null;
            if (File.Exists("sentry.bin"))
            {
                byte[] sentryFile = File.ReadAllBytes("sentry.bin");

                sentryHash = CryptoHelper.SHAHash(sentryFile);
                if (RememberMe && File.Exists("UserKey.bin"))
                {
                    byte[] keyhashed = File.ReadAllBytes("UserKey.bin");
                    RememberKey = Encoding.Unicode.GetString(CryptoHelper.SymmetricDecrypt(keyhashed, getstandardkey()));
                }

                if (decrwSentP == true)
                {
                    byte[] phashed = File.ReadAllBytes("UserPass.bin");
                    strPassword = Encoding.Unicode.GetString(CryptoHelper.SymmetricDecrypt(phashed, getstandardkey()));
                }
            }

            steamUser.LogOn(new SteamUser.LogOnDetails
            {
                Username = strUser,
                Password = strPassword,

                AuthCode = authCode,

                TwoFactorCode = twofactor,

                LoginKey = RememberKey,

                ShouldRememberPassword = RememberMe,

                SentryFileHash = sentryHash,
            });
        }

         bool failonce = false;

         void OnLoggedOn(SteamUser.LoggedOnCallback callback)
        {
            if (callback.Result == EResult.AccountLogonDenied)
            {
                Console.WriteLine("Steam Guard denied your a(cce)ss!", Console.ForegroundColor = ConsoleColor.Green);
                Console.WriteLine("Please enter code that you got emailed!", Console.ForegroundColor = ConsoleColor.Green);
                Console.ResetColor();
                Program.stallcommand = true;
                Console.Write("AuthCode> ");
                authCode = Console.ReadLine();
                Console.WriteLine("Your Authcode is: " + authCode);
                Program.stallcommand = false;
                return;
            }

            if (callback.Result == EResult.AccountLoginDeniedNeedTwoFactor)
            {
                Console.WriteLine("Steam Guard denied your a(cce)ss!", Console.ForegroundColor = ConsoleColor.Green);
                Console.WriteLine("Please enter code from two-factor authentication (ie. from your mobile app)!: ", Console.ForegroundColor = ConsoleColor.Green);
                Console.ResetColor();
                Program.stallcommand = true;
                Console.Write("Two-Factor Code> ");
                twofactor = Console.ReadLine();
                Program.stallcommand = false;
                return;
            }

            if (callback.Result == EResult.LogonSessionReplaced || callback.Result == EResult.LoggedInElsewhere || callback.Result == EResult.AlreadyLoggedInElsewhere)
            {
                Console.WriteLine("I'm already logged in elsewhere!  Shut down the other me first!", Console.ForegroundColor = ConsoleColor.Green);
                Console.ResetColor();
                steamClient.Disconnect();
                return;
            }

            if (callback.Result == EResult.InvalidPassword || callback.Result == EResult.IllegalPassword)
            {
                Console.WriteLine("Password error!  Please check your UserKey.bin, UserPass.bin, or UserPass.txt file and make sure it is intact.  Please enter a new password below!");
                if (!failonce)
                {
                    strPassword = GetUserPass(UserPassMode.EncryptedPassword);
                    failonce = true;
                }
                else strPassword = GetUserPass(UserPassMode.InputPassword);
                return;
            }

            if (callback.Result != EResult.OK)
            {
                Console.WriteLine("Login failed! Reason: " + callback.Result, Console.ForegroundColor = ConsoleColor.Green);
                return;
            }
            Console.WriteLine("I'm logged in and ready to go!", Console.ForegroundColor = ConsoleColor.Red);
            Console.Write("My SteamID: ");
            Console.WriteLine(steamClient.SteamID.ToString(), Console.ForegroundColor = ConsoleColor.Yellow);
            Console.Write("Owner SteamID (I'm assuming this is you.):", Console.ForegroundColor = ConsoleColor.Red);
            Console.WriteLine(BotOwnerID.ToString(), Console.ForegroundColor = ConsoleColor.Yellow);
            Console.ResetColor();
            authed = true;
        }

        /// <summary>
        /// Used for writing the file for SteamGuard.
        /// </summary>
        /// <param name="callback"></param>
         void UpdateMachineCallback(SteamUser.UpdateMachineAuthCallback callback)
        {
            Console.WriteLine("Updating sentry file....", Console.ForegroundColor = ConsoleColor.Green);

            byte[] sentryHash = CryptoHelper.SHAHash(callback.Data);

            File.WriteAllBytes("sentry.bin", callback.Data);

            steamUser.SendMachineAuthResponse(new SteamUser.MachineAuthDetails
            {
                JobID = callback.JobID,
                FileName = callback.FileName,
                BytesWritten = callback.BytesToWrite,
                FileSize = callback.Data.Length,
                Offset = callback.Offset,
                Result = EResult.OK,
                LastError = 0,
                OneTimePassword = callback.OneTimePassword,
                SentryFileHash = sentryHash,
            });

            Console.WriteLine("Done.", Console.ForegroundColor = ConsoleColor.Green);
            if (File.Exists("UserPass.bin"))
            {
                byte[] newpass = Encoding.Unicode.GetBytes(strPassword);
                byte[] newphashed = CryptoHelper.SymmetricEncrypt(newpass, getstandardkey());
                File.WriteAllBytes("UserPass.bin", newphashed);
                Console.WriteLine("UserPass.bin re-encrypted!", Console.ForegroundColor = ConsoleColor.Green);
                Console.ResetColor();
            }

            if (File.Exists("UserKey.bin") && RememberMe == true)
            {
                byte[] newpass = Encoding.Unicode.GetBytes(RememberKey);
                byte[] newphashed = CryptoHelper.SymmetricEncrypt(newpass, getstandardkey());
                File.WriteAllBytes("UserKey.bin", newphashed);
                Console.WriteLine("UserKey re-encrypted and stored!");
            }
        }

         void OnKeyGet(SteamUser.LoginKeyCallback callback)
        {
            if (RememberMe == true && File.Exists("sentry.bin"))
            {
                byte[] newpass = Encoding.Unicode.GetBytes(callback.LoginKey);
                byte[] newphashed = CryptoHelper.SymmetricEncrypt(newpass, getstandardkey());
                File.WriteAllBytes("UserKey.bin", newphashed);
                Console.WriteLine("UserKey encrypted and stored!", Console.ForegroundColor = ConsoleColor.Green);
                Console.ResetColor();
            }

        }


         void OnDisconnected(SteamKit2.SteamClient.DisconnectedCallback callback)
        {
            if (callback.UserInitiated)
            {
                Console.WriteLine("Disconnect successful.  I'll be napping over here until you reconnect.", Console.ForegroundColor = ConsoleColor.Green);
                steamIsRunning = false;
                return;
            }
            Console.WriteLine("Disconnected, reconnect in 5 sec.", Console.ForegroundColor = ConsoleColor.Green);
            Thread.Sleep(5000);
            steamClient.Connect();
        }

         void OnAccountInfo(SteamUser.AccountInfoCallback callback)
        {
            steamFriends.SetPersonaState(EPersonaState.Online);
        }

        /// <summary>
        /// Reacts to a friend message.  Currently will allow I, the one who created the bot, to kill it.
        /// </summary>
        /// <param name="callback"></param>
        public  void OnChatMessage(SteamFriends.FriendMsgCallback callback)
        {
            if (callback.Sender == BotOwnerID)
            {
                if (callback.Message == ">sleep")
                {
                    steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, "Bye, "+steamFriends.GetFriendPersonaName(BotOwnerID)+"!");
                    steamClient.Disconnect();
                    Environment.Exit(0);
                }
            }
            else steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, "Sorry, but I'm not ready to do anything right now.  Try again later.");
        }
    }
}
