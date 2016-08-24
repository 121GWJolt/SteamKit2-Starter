using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamKit2;
using System.Diagnostics;
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


namespace SteamKitServerUserTest
{   //MAKE SURE TO SET THE BOT OWNER ID TO YOUR OWN 64ID IN SteamIDList.cs.  Don't know it? Look it up on http://www.steamid.co
    class Program
    {
        /// <summary>
        /// Set this to true to temporarily stall the usual constant command input on the main console.
        /// </summary>
        public static bool stallcommand = false;
        static void Main(string[] args)
        {
            Console.WriteLine("Steambot is READY TO GO!");
            SteamKitServerUserTest.SteamConnect bot = new SteamConnect();
            Thread SteamClient = new Thread(() => bot.BeginClient(args));
            SteamClient.Start();
            while (true)
            {
                ConsoleKeyInfo inkey = Console.ReadKey(true);
                if (stallcommand == true)
                {
                    while (stallcommand == true)
                    Thread.Sleep(1000);
                }
                else
                {
                    Console.Write("Command> ");
                    string strCom =  Console.ReadLine();


                    if (bot.steamIsRunning && bot.steamClient.IsConnected && bot.authed)//Put any commands that require the SteamClient to be connected/logged to use in these brackets.
                    {

                    }


                    else if (strCom == ">sleep") Environment.Exit(0);
                    else if (strCom == ">debug") Console.WriteLine("I can see you!");
                    else if (strCom == ">DebugLog") bot.ToggleDLogToCon();
                    else if (strCom == ">ReConn")
                    {
                        if (bot.steamIsRunning == false)
                        {
                            SteamClient = new Thread(() => bot.BeginClient(args));
                            SteamClient.Start();
                        }
                        else
                        {
                            Console.WriteLine("Already connected.");
                        }
                    } 
                    else if (strCom == ">help")
                    {
                        Console.WriteLine("These are the current console commands as follows:");
                        foreach (string str in helparray)
                            Console.WriteLine(str);

                        
                    }
                }

            }
        }
        /// <summary>
        /// Help messages for the standard console commands.
        /// </summary>
        static string[] helparray = new string[]
        {
            ">sleep -- Immediately shut down the chatbot.  This is also accepted in friends chat, and will do the same thing if the bot owner types in the command.",
            ">debug  --  Make sure I can see your commands at the time.",
            ">DebugLog -- Outputs messages from SteamKit2's DebugLog function to console.  Always outputs to VS's Debug Window in debug mode, but this can output it to console regardless of the current mode.",
            ">ReConn -- If a manual disconnect occurred before, you can reconnect with this command.",
            ">help -- Display this list of commands.  It can be edited in Program.cs at design time."
        };
    }
}