using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using LibGit2Sharp;

namespace CraftopiaNedclanSync
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("##########################################################################");
            Console.WriteLine("#                                                                        #");
            Console.WriteLine("#    d8b   db d88888b d8888b.         .o88b. db       .d8b.  d8b   db    #"); 
            Console.WriteLine("#    888o  88 88'     88  `8D        d8P  Y8 88      d8' `8b 888o  88    #"); 
            Console.WriteLine("#    88V8o 88 88ooooo 88   88        8P      88      88ooo88 88V8o 88    #"); 
            Console.WriteLine("#    88 V8o88 88~~~~~ 88   88 C8888D 8b      88      88~~~88 88 V8o88    #"); 
            Console.WriteLine("#    88  V888 88.     88  .8D        Y8b  d8 88booo. 88   88 88  V888    #"); 
            Console.WriteLine("#    VP   V8P Y88888P Y8888D'         `Y88P' Y88888P YP   YP VP   V8P    #");
            Console.WriteLine("#                                                                        #");
            Console.WriteLine("##########################################################################");
            Console.WriteLine("#                                                                        #");
            Console.WriteLine("#               Craftopia server sync - www.ned-clan.com                   #");
            Console.WriteLine("#                                                                        #");
            Console.WriteLine("##########################################################################");
            Console.WriteLine("");

            // Some variables we'll need
            var remoteGitUri = "https://github.com/martijns/CraftopiaNedclanBinaries.git";
            var exePath = Environment.ProcessPath;
            var exeDir = Path.GetDirectoryName(exePath);
            var exeFilename = Path.GetFileName(exePath);
            var dotGitDir = Path.Combine(exeDir, ".git");

            // Check any left-over old files from the update process
            if (File.Exists(exePath + ".bak"))
            {
                try
                {
                    File.Delete(exePath + ".bak");
                    Console.WriteLine("Cleaned old file left behind after update");
                }
                catch (Exception)
                {
                    Console.WriteLine($"Found old executable at '{exePath + ".bak"}', but failed to clear right now, will retry next time...");
                }
            }

            // Create empty git repo if it doesn't exist yet
            if (!Directory.Exists(dotGitDir))
            {
                Console.WriteLine($"No git repository found, initializing one in '{exeDir}'");
                Repository.Init(exeDir);
            }

            // Determine the most recent commit
            var references = Repository.ListRemoteReferences(remoteGitUri);
            var commithash = references.Where(r => r.CanonicalName == "HEAD").First().ResolveToDirectReference().TargetIdentifier;
            Console.WriteLine($"Most recent remote commit is '{commithash}'");

            // Update git
            using (var repo = new Repository(exeDir))
            {
                // Make sure it has our remote
                if (!repo.Network.Remotes.Any(r => r.Name == "origin"))
                {
                    repo.Network.Remotes.Add("origin", remoteGitUri);
                    Console.WriteLine($"Added remote 'origin' with uri: {remoteGitUri}");
                }

                // Fetch updates from remote
                var remote = repo.Network.Remotes["origin"];
                var refspec = remote.FetchRefSpecs.Select(x => x.Specification);
                Console.WriteLine($"Fetching remote files from '{remote.Name}'");
                Commands.Fetch(repo, remote.Name, refspec, null, null);

                // Use Reset to checkout (and discard any changes) to our latest commit
                Console.WriteLine($"Resetting to most recent commit '{commithash}'");
                var latestCommit = repo.Lookup<Commit>(commithash);
                repo.Reset(ResetMode.Hard, latestCommit, new CheckoutOptions {
                    OnCheckoutProgress = (path, completedSteps, totalSteps) => {
                        Console.WriteLine($"[{completedSteps}/{totalSteps}] {path}");
                    }
                });
                Console.WriteLine($"Done.");
            }

            // Did we get an update for ourselves?
            var updatedExe = exePath + ".update";
            if (File.Exists(updatedExe) && CalculateFileHash(updatedExe) != CalculateFileHash(exePath))
            {
                Console.WriteLine($"There's an update for the updater, applying and starting it...");
                File.Move(exePath, exePath + ".bak", true);
                File.Move(exePath + ".update", exePath);
                Process.Start(exePath, args);
                return;
            }

            // Start the game
            var gameExeWindows = Path.Combine(exeDir, "Craftopia.exe");
            var gameExeLinuxServer = Path.Combine(exeDir, "Craftopia.x86_64");
            var gameExe = new[] {gameExeWindows, gameExeLinuxServer}.Where(x => File.Exists(x)).FirstOrDefault();
            if (gameExe != null)
            {
                Console.WriteLine($"Found game executable, starting: {gameExe} {string.Join(" ", args)}");
                var gameProcess = Process.Start(gameExe, args);
                Console.WriteLine($"Game exited or started in background!");
            }
        }

        static string CalculateFileHash(string path)
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            using var bs = new BufferedStream(fs);
            using var sha1 = SHA1.Create();
            byte[] hash = sha1.ComputeHash(bs);
            return BitConverter.ToString(hash);
        }

    }
}
